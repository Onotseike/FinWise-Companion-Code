using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;

namespace FinWise.MauiApp.Services;

/// <summary>
/// HTTP client for communicating with the SupervisorAgent Azure Functions.
/// Provides a unified interface for chat and routing endpoints.
/// All responses include comprehensive telemetry data: agent hops, function calls, tool invocations, and detailed token usage.
/// This enables full observability of multi-agent orchestration.
/// </summary>
public class SupervisorAgentHttpClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
        }
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _functionKey;

    public SupervisorAgentHttpClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

#if ANDROID
        // Android emulator uses 10.0.2.2 to reach host machine's localhost
        _baseUrl = "http://10.0.2.2:7072"; // Default Azure Functions port for SupervisorAgent
#elif IOS
        // iOS Simulator can use localhost directly, but we prefer explicit IP for reliability
        // Alternative: Use the machine the azure function is running on. If it is the same machine, use your machine's IP address (e.g., "http://192.168.1.100:7072")
        _baseUrl = configuration["SupervisorAgent:BaseUrl"] ?? "http://localhost:7072";
#else
        _baseUrl = configuration["SupervisorAgent:BaseUrl"] ?? "http://localhost:7072";
#endif

        _functionKey = configuration["SupervisorAgent:FunctionKey"] ?? "";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        if (!string.IsNullOrEmpty(_functionKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _functionKey);
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(120); // Longer timeout for multi-agent coordination
    }

    /// <summary>
    /// Send a chat message through the Supervisor Agent.
    /// The supervisor will route to the appropriate specialist agent and return the response
    /// with full telemetry including agent hops, function calls, tool invocations, and detailed token usage.
    /// </summary>
    public async Task<EnhancedSupervisorResponse> ChatAsync(SupervisorChatRequest request)
    {
        try
        {
            string json = JsonSerializer.Serialize(request, SerializerOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/supervisor/chat", content);
            _ = response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EnhancedSupervisorResponse>(responseContent, DeserializerOptions);

            return result ?? throw new InvalidOperationException("Failed to deserialize enhanced supervisor response");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Cannot connect to Supervisor Agent at {_baseUrl}. " +
                $"Ensure Azure Functions is running on port 7072. " +
                $"iOS Simulator users: Try using your Mac's IP address instead of localhost. " +
                $"Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error communicating with supervisor: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Send a message directly to a specific agent, bypassing automatic routing.
    /// Routes the request through /api/supervisor/route/{agentName}.
    /// Returns the response with full telemetry data.
    /// </summary>
    /// <param name="agentName">The target agent name (e.g., "budgeting", "loan")</param>
    /// <param name="request">The routing request with message and metadata</param>
    /// <returns>Enhanced supervisor response with full telemetry data</returns>
    public async Task<EnhancedSupervisorResponse> RouteRequestAsync(string agentName, SupervisorRouteRequest request)
    {
        try
        {
            string json = JsonSerializer.Serialize(request, SerializerOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/supervisor/route/{agentName}", content);
            _ = response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EnhancedSupervisorResponse>(responseContent, DeserializerOptions);

            return result ?? throw new InvalidOperationException("Failed to deserialize enhanced route response");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Cannot connect to Supervisor Agent at {_baseUrl}. " +
                $"Ensure Azure Functions is running on port 7072. " +
                $"Error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error routing to agent '{agentName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get health status of the supervisor and available agents.
    /// </summary>
    public async Task<SupervisorHealthResponse> GetHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/supervisor/health");
            _ = response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SupervisorHealthResponse>(responseContent, DeserializerOptions) ?? new SupervisorHealthResponse();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error checking supervisor health: {ex.Message}", ex);
        }
    }
}
