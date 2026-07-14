using System.Net;
using System.Text.Json;

using FinWise.LoanAgent.Contracts;
using FinWise.LoanAgent.Models;
using FinWise.LoanAgent.Services;
using FinWise.Shared.Core.Models;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FinWise.LoanAgent;

/// <summary>
/// Modernized HTTP endpoint functions for the Loan Agent.
/// Provides mortgage analysis, loan scenarios, and property evaluation.
/// </summary>
public class LoanAgentFunctions(LoanAgentOrchestrator agentService, IMortgageAgent mortgageAgent, ILogger<LoanAgentFunctions> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() { WriteIndented = true };

    private readonly LoanAgentOrchestrator _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
    private readonly IMortgageAgent _mortgageAgent = mortgageAgent ?? throw new ArgumentNullException(nameof(mortgageAgent));
    private readonly ILogger<LoanAgentFunctions> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #region Private Helper Methods

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        string message)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new { error = message });
        return response;
    }

    private static async Task<HttpResponseData> CreateSuccessResponseAsync(
        HttpRequestData request,
        object? data)
    {
        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(data);
        return response;
    }

    private static object? DeserializeResponseData(string json) => JsonSerializer.Deserialize<object>(json, JsonOptions);

    #endregion

    #region Health Check

    /// <summary>
    /// GET /api/loan/health
    /// Health check endpoint.
    /// </summary>
    [Function("LoanHealth")]
    public async Task<HttpResponseData> HealthAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "loan/health")] HttpRequestData request)
    {
        _logger.LogInformation("Loan Agent health check endpoint invoked");

        var response = request.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            status = "healthy",
            agent = "LoanAgent",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
        return response;
    }

    #endregion

    #region Loan Analysis Endpoints

    /// <summary>
    /// POST /api/loan/analyze-mortgage
    /// Analyze mortgage options based on user query.
    /// </summary>
    [Function("LoanAnalyzeMortgage")]
    public async Task<HttpResponseData> AnalyzeMortgageAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "loan/analyze-mortgage")] HttpRequestData request,
        FunctionContext context)
    {
        _logger.LogInformation("Loan analysis endpoint invoked");

        try
        {
            var body = await request.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Request body is required");
            }

            var loanRequest = JsonSerializer.Deserialize<LoanAnalysisRequest>(body, JsonOptions);

            if (loanRequest == null || string.IsNullOrWhiteSpace(loanRequest.Message))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Message field is required");
            }

            // Create loan context
            var loanContext = new LoanAgentContext
            {
                UserId = loanRequest.UserId ?? "anonymous",
                ConversationId = loanRequest.ConversationId ?? Guid.NewGuid().ToString(),
                UserMessage = loanRequest.Message,
                Operation = "analyze_mortgage",
                TraceId = Guid.NewGuid().ToString(),
                SpanId = Guid.NewGuid().ToString()
            };

            // Analyze mortgage options
            var response = await _agentService.AnalyzeMortgageOptionsAsync(loanContext);

            var httpResponse = request.CreateResponse(HttpStatusCode.OK);
            var loanResponse = new LoanAnalysisResponse
            {
                ConversationId = loanContext.ConversationId,
                Analysis = response,
                TraceId = loanContext.TraceId,
                SpanId = loanContext.SpanId
            };

            await httpResponse.WriteAsJsonAsync(loanResponse);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in loan analysis endpoint");
            return await CreateErrorResponseAsync(request, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// POST /api/loan/mortgage-scenario
    /// Evaluate a specific mortgage scenario.
    /// </summary>
    [Function("LoanMortgageScenario")]
    public async Task<HttpResponseData> EvaluateMortgageScenarioAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "loan/mortgage-scenario")] HttpRequestData request,
        FunctionContext context)
    {
        _logger.LogInformation("Mortgage scenario endpoint invoked");

        try
        {
            var body = await request.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Request body is required");
            }

            var scenarioRequest = JsonSerializer.Deserialize<LoanScenarioRequest>(body, JsonOptions);

            if (scenarioRequest == null)
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Valid scenario request is required");
            }

            var loanContext = new LoanAgentContext
            {
                UserId = scenarioRequest.UserId ?? "anonymous",
                ConversationId = scenarioRequest.ConversationId ?? Guid.NewGuid().ToString(),
                Operation = "evaluate_scenario",
                TraceId = Guid.NewGuid().ToString(),
                SpanId = Guid.NewGuid().ToString()
            };

            var scenario = await _agentService.EvaluateMortgageScenarioAsync(loanContext, scenarioRequest);

            var httpResponse = request.CreateResponse(HttpStatusCode.OK);
            var response = new LoanScenarioResponse
            {
                ConversationId = loanContext.ConversationId,
                Scenario = scenario,
                TraceId = loanContext.TraceId
            };

            await httpResponse.WriteAsJsonAsync(response);
            return httpResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in mortgage scenario endpoint");
            return await CreateErrorResponseAsync(request, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #endregion

    #region Property Data Endpoints

    private async Task<string> GetPropertiesAsync(string country)
    {
        try
        {
            if (!Enum.TryParse<SupportedCountry>(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
            {
                return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
            }
            IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetAllAsync(supportedCountry);
            return JsonSerializer.Serialize(properties, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetPropertiesAsync");
            throw new InvalidOperationException($"Error fetching properties: {ex.Message}", ex);
        }
    }

    private async Task<string> GetPropertiesByLocationAsync(string country, string location)
    {
        try
        {
            if (!Enum.TryParse<SupportedCountry>(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
            {
                return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
            }
            if (string.IsNullOrWhiteSpace(location))
            {
                return "Error: Location must be provided.";
            }
            IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetByLocationAsync(supportedCountry, location);
            return JsonSerializer.Serialize(properties, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetPropertiesByLocationAsync");
            throw new InvalidOperationException($"Error fetching properties by location: {ex.Message}", ex);
        }
    }

    private async Task<string> GetPropertiesByPriceRangeAsync(string country, decimal minPrice, decimal maxPrice, string? location = null)
    {
        try
        {
            if (!Enum.TryParse<SupportedCountry>(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
            {
                return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
            }
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                return "Error: Invalid price range.";
            }
            IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetByPriceRangeAsync(supportedCountry, minPrice, maxPrice, location);
            return JsonSerializer.Serialize(properties, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetPropertiesByPriceRangeAsync");
            throw new InvalidOperationException($"Error fetching properties by price range: {ex.Message}", ex);
        }
    }

    private async Task<string> GetPropertiesByFiltersAsync(string country, Dictionary<string, object> filters)
    {
        try
        {
            if (!Enum.TryParse<SupportedCountry>(country, true, out SupportedCountry supportedCountry) || !_mortgageAgent.Properties.SupportedCountries.Contains(supportedCountry))
            {
                return $"Error: Unsupported country '{country}'. Supported countries are: {string.Join(", ", _mortgageAgent.Properties.SupportedCountries)}";
            }
            if (filters == null || filters.Count == 0)
            {
                return "Error: At least one filter must be provided.";
            }
            IEnumerable<PropertyPriceBaseRecord> properties = await _mortgageAgent.Properties.GetFilteredAsync(supportedCountry, filters);
            return JsonSerializer.Serialize(properties, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetPropertiesByFiltersAsync");
            throw new InvalidOperationException($"Error fetching properties by filters: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// GET /api/loan/properties/{country}
    /// Get all properties for a specified country.
    /// </summary>
    [Function("GetPropertiesHttp")]
    public async Task<HttpResponseData> GetPropertiesHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "loan/properties/{country}")] HttpRequestData request,
        string country)
    {
        try
        {
            string result = await GetPropertiesAsync(country);
            return await CreateSuccessResponseAsync(request, DeserializeResponseData(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPropertiesHttpAsync");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// GET /api/loan/properties/{country}/location/{location}
    /// Get properties by country and location.
    /// </summary>
    [Function("GetPropertiesByLocationHttp")]
    public async Task<HttpResponseData> GetPropertiesByLocationHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "loan/properties/{country}/location/{location}")] HttpRequestData request,
        string country,
        string location)
    {
        try
        {
            string result = await GetPropertiesByLocationAsync(country, location);
            return await CreateSuccessResponseAsync(request, DeserializeResponseData(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPropertiesByLocationHttpAsync");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// GET /api/loan/properties/{country}/price-range
    /// Get properties by country within a price range.
    /// </summary>
    [Function("GetPropertiesByPriceRangeHttp")]
    public async Task<HttpResponseData> GetPropertiesByPriceRangeHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "loan/properties/{country}/price-range")] HttpRequestData request,
        string country)
    {
        try
        {
            if (!decimal.TryParse(request.Query["minPrice"], out decimal minPrice) || !decimal.TryParse(request.Query["maxPrice"], out decimal maxPrice))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Invalid or missing minPrice/maxPrice query parameters.");
            }
            string? location = request.Query["location"];
            string result = await GetPropertiesByPriceRangeAsync(country, minPrice, maxPrice, string.IsNullOrWhiteSpace(location) ? null : location);
            return await CreateSuccessResponseAsync(request, DeserializeResponseData(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPropertiesByPriceRangeHttpAsync");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }
    }

    /// <summary>
    /// POST /api/loan/properties/{country}/filters
    /// Get properties by country with specific filters.
    /// </summary>
    [Function("GetPropertiesByFiltersHttp")]
    public async Task<HttpResponseData> GetPropertiesByFiltersHttpAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "loan/properties/{country}/filters")] HttpRequestData request,
        string country)
    {
        try
        {
            string? requestBody = await request.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Request body must contain a JSON object with at least one filter.");
            }
            var filters = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody, JsonOptions);
            if (filters == null || filters.Count == 0)
            {
                return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, "Request body must contain a JSON object with at least one filter.");
            }
            string result = await GetPropertiesByFiltersAsync(country, filters);
            return await CreateSuccessResponseAsync(request, DeserializeResponseData(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPropertiesByFiltersHttpAsync");
            return await CreateErrorResponseAsync(request, HttpStatusCode.BadRequest, ex.Message);
        }
    }
    #endregion
}
