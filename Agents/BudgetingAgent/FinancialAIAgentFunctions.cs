using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using FinWise.BudgetingAgent.Services;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Net;
using Microsoft.Agents.AI;

namespace FinWise.BudgetingAgent;

public class FinancialAIAgentFunctions(FinancialsAIAgent aiAgent, ILogger<FinancialAIAgentFunctions> logger)
{
    private readonly FinancialsAIAgent _aiAgent = aiAgent ?? throw new ArgumentNullException(nameof(aiAgent));
    private readonly ILogger<FinancialAIAgentFunctions> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Creates a structured logging scope that stamps every log entry inside a method with
    /// ClassName, MethodName, and CallerLineNumber (resolved at compile time — zero runtime cost).
    /// Usage: <c>using var scope = BeginMethodScope();</c> at the top of each method.
    /// All log calls within the using block automatically inherit these properties in
    /// Application Insights and any structured sink (Seq, ELK, etc.).
    /// </summary>
    private IDisposable? BeginMethodScope(
        [CallerMemberName] string methodName = "",
        [CallerLineNumber] int lineNumber = 0) =>
        _logger.BeginScope(new Dictionary<string, object?>
        {
            ["ClassName"] = nameof(FinancialAIAgentFunctions),
            ["MethodName"] = methodName,
            ["CallerLineNumber"] = lineNumber
        });

    private AgentHttpResponse CreateAgentHttpResponse(AgentResponse response, int estimatedInputTokens = 0)
    {
        var usage = response.Usage;
        bool isUsageAvailable = usage is not null;
        int? exactInputTokens = (int?)usage?.InputTokenCount;
        int? exactOutputTokens = (int?)usage?.OutputTokenCount;
        int? exactTotalTokens = (int?)usage?.TotalTokenCount ?? (exactInputTokens + exactOutputTokens);
        int estimatedOutputTokens = EstimateTokenCount(response.Text);
        int estimatedTotalTokens = estimatedInputTokens + estimatedOutputTokens;
        int measuredInputTokens = ResolveMeasuredTokens(estimatedInputTokens, exactInputTokens);
        int measuredOutputTokens = ResolveMeasuredTokens(estimatedOutputTokens, exactOutputTokens);
        int measuredTotalTokens = ResolveMeasuredTokens(estimatedTotalTokens, exactTotalTokens, measuredInputTokens + measuredOutputTokens);

        return new AgentHttpResponse(
            Text: response.Text,
            ResponseId: response.ResponseId,
            AgentId: _aiAgent.AgentId,
            CreatedAt: response.CreatedAt,
            MessageCount: response.Messages.Count,
            TokenMeasurementMode: _aiAgent.TokenMeasurementMode,
            ExactUsageAvailable: isUsageAvailable,
            ExactUsageSource: "AgentResponse.Usage",
            MeasuredInputTokens: measuredInputTokens,
            MeasuredOutputTokens: measuredOutputTokens,
            MeasuredTotalTokens: measuredTotalTokens,
            ExactInputTokens: exactInputTokens,
            ExactOutputTokens: exactOutputTokens,
            ExactTotalTokens: exactTotalTokens,
            EstimatedInputTokens: estimatedInputTokens,
            EstimatedOutputTokens: estimatedOutputTokens,
            EstimatedTotalTokens: estimatedTotalTokens);
    }

    private int ResolveMeasuredTokens(int estimated, int? exact, int? fallback = null) => _aiAgent.TokenMeasurementMode switch
    {
        "exact" => exact ?? fallback ?? estimated,
        "estimate" => estimated,
        _ => exact ?? fallback ?? estimated
    };

    private static int EstimateTokenCount(string? text) => string.IsNullOrWhiteSpace(text) ? 0 : (int)Math.Ceiling(text.Length / 4.0); // Rough heuristic: 1 token ≈ 4 characters in English. Adjust as needed for other languages or content types. see https://help.openai.com/en/articles/4936856-what-are-tokens-and-how-to-count-them

    private static string ResolveExperimentPhase(HttpRequestData req)
    {
        if (req.Headers.TryGetValues("x-experiment-phase", out var values))
        {
            var headerPhase = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerPhase))
            {
                return headerPhase.Trim();
            }
        }

        var queryPhase = req.Query["phase"];
        return string.IsNullOrWhiteSpace(queryPhase) ? "baseline" : queryPhase;
    }

    private static string ResolveCorrelationId(HttpRequestData req, string headerName, string prefix)
    {
        if (req.Headers.TryGetValues(headerName, out var values))
        {
            var headerValue = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.Trim();
            }
        }

        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private static async Task<HttpResponseData> CreateJsonResponseAsync(
        HttpRequestData request,
        HttpStatusCode statusCode,
        object payload)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(payload);
        return response;
    }

    [Function(nameof(ChatWithFinancialAdvisor))]
    public async Task<HttpResponseData> ChatWithFinancialAdvisor(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ai/chat")] HttpRequestData req)
    {
        using var scope = BeginMethodScope();
        try
        {
            string experimentPhase = ResolveExperimentPhase(req);
            string workflowId = ResolveCorrelationId(req, "x-workflow-id", "wf");
            string hopId = ResolveCorrelationId(req, "x-hop-id", "hop");
            _logger.LogInformation("Handling request. Route={Route}, HttpMethod={HttpMethod}", "ai/chat", req.Method);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            ChatRequest? chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody);

            if (string.IsNullOrWhiteSpace(chatRequest?.Message))
            {
                _logger.LogWarning(
                    "Bad request: Message field is missing or empty. Route={Route}",
                    "ai/chat");
                return await CreateJsonResponseAsync(req, HttpStatusCode.BadRequest, new { error = "Message is required" });
            }

            _logger.LogInformation(
                "Invoking agent. Route={Route}, MessageLength={MessageLength}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/chat", chatRequest.Message.Length, experimentPhase, workflowId, hopId);

            AgentResponse response = await _aiAgent.AnalyzeFinancialHealthAsync(chatRequest.Message, "ai/chat", experimentPhase, "chat", workflowId, hopId);

            _logger.LogInformation(
                "Request completed. Route={Route}, ResponseId={ResponseId}, MessageCount={MessageCount}, Status={StatusCode}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/chat", response.ResponseId, response.Messages.Count, (int)HttpStatusCode.OK, experimentPhase, workflowId, hopId);

            int estimatedInputTokens = EstimateTokenCount(chatRequest.Message);
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, CreateAgentHttpResponse(response, estimatedInputTokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed. Route={Route}, ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}",
                "ai/chat", ex.GetType().Name, ex.Message);
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    [Function(nameof(GetBudgetAdvice))]
    public async Task<HttpResponseData> GetBudgetAdvice(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ai/budget-advice")] HttpRequestData req)
    {
        using var scope = BeginMethodScope();
        try
        {
            string experimentPhase = ResolveExperimentPhase(req);
            string workflowId = ResolveCorrelationId(req, "x-workflow-id", "wf");
            string hopId = ResolveCorrelationId(req, "x-hop-id", "hop");
            _logger.LogInformation("Handling request. Route={Route}, HttpMethod={HttpMethod}", "ai/budget-advice", req.Method);

            AgentResponse advice = await _aiAgent.ProvideBudgetAdviceAsync("ai/budget-advice", experimentPhase, "budget-advice", workflowId, hopId);

            _logger.LogInformation(
                "Request completed. Route={Route}, ResponseId={ResponseId}, MessageCount={MessageCount}, Status={StatusCode}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/budget-advice", advice.ResponseId, advice.Messages.Count, (int)HttpStatusCode.OK, experimentPhase, workflowId, hopId);

            // No user input for budget advice endpoint, so estimated input tokens is 0
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, CreateAgentHttpResponse(advice, estimatedInputTokens: 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed. Route={Route}, ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}",
                "ai/budget-advice", ex.GetType().Name, ex.Message);
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    [Function(nameof(AnalyzeSpending))]
    public async Task<HttpResponseData> AnalyzeSpending(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ai/analyze-spending")] HttpRequestData req)
    {
        using var scope = BeginMethodScope();
        try
        {
            string timeframe = req.Query["timeframe"] ?? "last-month";
            string experimentPhase = ResolveExperimentPhase(req);
            string workflowId = ResolveCorrelationId(req, "x-workflow-id", "wf");
            string hopId = ResolveCorrelationId(req, "x-hop-id", "hop");

            _logger.LogInformation(
                "Handling request. Route={Route}, HttpMethod={HttpMethod}, Timeframe={Timeframe}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/analyze-spending", req.Method, timeframe, experimentPhase, workflowId, hopId);

            AgentResponse analysis = await _aiAgent.AnalyzeSpendingPatternsAsync(timeframe, "ai/analyze-spending", experimentPhase, $"analyze-spending-{timeframe}", workflowId, hopId);

            _logger.LogInformation(
                "Request completed. Route={Route}, Timeframe={Timeframe}, ResponseId={ResponseId}, MessageCount={MessageCount}, Status={StatusCode}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/analyze-spending", timeframe, analysis.ResponseId, analysis.Messages.Count, (int)HttpStatusCode.OK, experimentPhase, workflowId, hopId);

            int estimatedInputTokens = EstimateTokenCount(timeframe);
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, CreateAgentHttpResponse(analysis, estimatedInputTokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed. Route={Route}, ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}",
                "ai/analyze-spending", ex.GetType().Name, ex.Message);
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    [Function(nameof(CreatePersonalizedBudget))]
    public async Task<HttpResponseData> CreatePersonalizedBudget(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "ai/create-budget")] HttpRequestData req)
    {
        using var scope = BeginMethodScope();
        try
        {
            string experimentPhase = ResolveExperimentPhase(req);
            string workflowId = ResolveCorrelationId(req, "x-workflow-id", "wf");
            string hopId = ResolveCorrelationId(req, "x-hop-id", "hop");
            _logger.LogInformation("Handling request. Route={Route}, HttpMethod={HttpMethod}", "ai/create-budget", req.Method);

            AgentResponse budget = await _aiAgent.CreatePersonalizedBudgetAsync("ai/create-budget", experimentPhase, "create-budget", workflowId, hopId);

            _logger.LogInformation(
                "Request completed. Route={Route}, ResponseId={ResponseId}, MessageCount={MessageCount}, Status={StatusCode}, ExperimentPhase={ExperimentPhase}, WorkflowId={WorkflowId}, HopId={HopId}",
                "ai/create-budget", budget.ResponseId, budget.Messages.Count, (int)HttpStatusCode.OK, experimentPhase, workflowId, hopId);

            // No explicit user input for create-budget endpoint, so estimated input tokens is 0
            return await CreateJsonResponseAsync(req, HttpStatusCode.OK, CreateAgentHttpResponse(budget, estimatedInputTokens: 0));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Request failed. Route={Route}, ExceptionType={ExceptionType}, ExceptionMessage={ExceptionMessage}",
                "ai/create-budget", ex.GetType().Name, ex.Message);
            return await CreateJsonResponseAsync(req, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    public record ChatRequest(string Message);

    public record AgentHttpResponse(
        string Text,
        string? ResponseId,
        string? AgentId,
        DateTimeOffset? CreatedAt,
        int MessageCount,
        string TokenMeasurementMode,
        bool ExactUsageAvailable,
        string ExactUsageSource,
        int MeasuredInputTokens,
        int MeasuredOutputTokens,
        int MeasuredTotalTokens,
        int? ExactInputTokens,
        int? ExactOutputTokens,
        int? ExactTotalTokens,
        int EstimatedInputTokens,
        int EstimatedOutputTokens,
        int EstimatedTotalTokens);
}