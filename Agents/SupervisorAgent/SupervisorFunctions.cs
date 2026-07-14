using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using FinWise.Shared.Core.Telemetry;
using FinWise.SupervisorAgent.Models;
using FinWise.SupervisorAgent.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinWise.SupervisorAgent;

/// <summary>
/// Azure Functions entry points for the Supervisor Agent.
/// Routes requests to appropriate specialist agents (BudgetingAgent, LoanAgent, etc.)
/// </summary>
public class SupervisorFunctions(
    ILogger<SupervisorFunctions> logger,
    IServiceProvider serviceProvider)
{
    private static readonly ConcurrentDictionary<string, List<ConversationMessage>> s_conversationHistory = new(StringComparer.Ordinal);
    private readonly ILogger<SupervisorFunctions> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true)
        }
    };

    /// <summary>
    /// Health check endpoint for the Supervisor Agent.
    /// </summary>
    [Function("SupervisorHealth")]
    public async Task<HttpResponseData> HealthCheckAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "supervisor/health")] HttpRequestData req)
    {
        _logger.LogInformation("Supervisor Agent health check triggered");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync("{\"status\":\"healthy\",\"agent\":\"SupervisorAgent\",\"version\":\"1.0.0\"}");
        return response;
    }

    /// <summary>
    /// Main chat endpoint. Accepts <c>{ "userId": "...", "message": "..." }</c> and
    /// routes through the <see cref="SupervisorAgentOrchestrator"/>.
    /// </summary>
    [Function("SupervisorChat")]
    public async Task<HttpResponseData> SupervisorChat(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "supervisor/chat")] HttpRequestData req)
    {
        _logger.LogInformation("SupervisorChat function triggered");

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("{\"error\":\"Request body is required\"}");
                return bad;
            }

            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, _jsonOptions);
            if (chatRequest is null || string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("{\"error\":\"'message' field is required\"}");
                return bad;
            }

            string conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString("N");
            List<ConversationMessage> messageHistory = GetConversationHistory(conversationId);
            messageHistory.Add(new ConversationMessage { Role = "user", Content = chatRequest.Message, Timestamp = DateTime.UtcNow });

            var context = new SupervisorContext
            {
                UserId = chatRequest.UserId ?? "anonymous",
                UserMessage = chatRequest.Message,
                ConversationId = conversationId,
                TraceId = Guid.NewGuid().ToString("N"),
                MessageHistory = messageHistory,
                TokenMeasurementMode = ResolveTokenMeasurementMode(chatRequest)
            };

            var orchestrator = _serviceProvider.GetRequiredService<SupervisorAgentOrchestrator>();
            string result = await orchestrator.OrchestrateMulitAgentRequestAsync(context);
            context.MessageHistory.Add(new ConversationMessage { Role = "assistant", Content = result, Timestamp = DateTime.UtcNow });
            SaveConversationHistory(context.ConversationId, context.MessageHistory);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(JsonSerializer.Serialize(CreateResponsePayload(context, result), _jsonOptions));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing supervisor chat request");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync($"{{\"error\":\"{ex.Message}\"}}");
            return err;
        }
    }

    /// <summary>
    /// Direct-route endpoint: <c>POST /supervisor/route/{agentName}</c>.
    /// Bypasses intent classification and delegates straight to the named agent.
    /// Accepts <c>{ "userId": "...", "message": "..." }</c>.
    /// </summary>
    [Function("SupervisorRoute")]
    public async Task<HttpResponseData> RouteRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "supervisor/route/{agentName}")] HttpRequestData req,
        string agentName)
    {
        _logger.LogInformation("Route request triggered for agent: {AgentName}", agentName);

        try
        {
            var requestBody = await req.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("{\"error\":\"Request body is required\"}");
                return bad;
            }

            if (!AgentRegistry.IsAgentAvailable(agentName))
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync($"{{\"error\":\"Agent '{agentName}' not registered\"}}");
                return notFound;
            }

            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, _jsonOptions);
            if (chatRequest is null || string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("{\"error\":\"'message' field is required\"}");
                return bad;
            }

            // Build a context with a pre-set route so the orchestrator skips LLM intent classification
            string conversationId = chatRequest.ConversationId ?? Guid.NewGuid().ToString("N");
            List<ConversationMessage> messageHistory = GetConversationHistory(conversationId);
            messageHistory.Add(new ConversationMessage { Role = "user", Content = chatRequest.Message, Timestamp = DateTime.UtcNow });

            var context = new SupervisorContext
            {
                UserId = chatRequest.UserId ?? "anonymous",
                UserMessage = chatRequest.Message,
                ConversationId = conversationId,
                TraceId = Guid.NewGuid().ToString("N"),
                MessageHistory = messageHistory,
                TokenMeasurementMode = ResolveTokenMeasurementMode(chatRequest),
                CurrentRoute = new IntentRoute
                {
                    TargetAgent = agentName.ToLowerInvariant(),
                    Confidence = 1.0m,
                    IntentCategory = "direct_route"
                }
            };

            var orchestrator = _serviceProvider.GetRequiredService<SupervisorAgentOrchestrator>();
            string result = await orchestrator.OrchestrateMulitAgentRequestAsync(context);
            context.MessageHistory.Add(new ConversationMessage { Role = "assistant", Content = result, Timestamp = DateTime.UtcNow });
            SaveConversationHistory(context.ConversationId, context.MessageHistory);

            var ok = req.CreateResponse(HttpStatusCode.OK);
            ok.Headers.Add("Content-Type", "application/json");
            await ok.WriteStringAsync(JsonSerializer.Serialize(CreateResponsePayload(context, result, agentName), _jsonOptions));
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing request to {AgentName}", agentName);
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync($"{{\"error\":\"{ex.Message}\"}}");
            return err;
        }
    }

    private static object CreateResponsePayload(SupervisorContext context, string responseText, string? forcedAgent = null)
    {
        var topics = new List<string>();

        if (!string.IsNullOrWhiteSpace(context.CurrentRoute?.IntentCategory))
            topics.Add(context.CurrentRoute.IntentCategory);

        if (!string.IsNullOrWhiteSpace(context.CurrentRoute?.TargetAgent))
            topics.Add(context.CurrentRoute.TargetAgent);

        if (context.CurrentRoute?.SecondaryAgents is { Count: > 0 })
        {
            foreach (var secondary in context.CurrentRoute.SecondaryAgents)
            {
                if (!string.IsNullOrWhiteSpace(secondary))
                    topics.Add(secondary);
            }
        }

        // Aggregate total tokens: supervisor + downstream
        var totalTokens = new FinWise.Shared.Core.Telemetry.TokenMeasurement();
        if (context.SupervisorTokens != null && context.DownstreamTokens != null)
        {
            totalTokens = FinWise.Shared.Core.Telemetry.TokenMeasurement.Aggregate(
                context.SupervisorTokens,
                context.DownstreamTokens);
        }
        else if (context.SupervisorTokens != null)
        {
            totalTokens = context.SupervisorTokens;
        }
        else if (context.DownstreamTokens != null)
        {
            totalTokens = context.DownstreamTokens;
        }
        totalTokens.Mode = context.TokenMeasurementMode;

        string NormalizeToRequestType(string messageType) =>
            messageType.EndsWith(".Result", StringComparison.OrdinalIgnoreCase)
                ? messageType[..^7]
                : messageType;

        Dictionary<string, AgentHopTelemetry> telemetryByAgentAndType = context.AgentHops
            .ToDictionary(
                h => $"{h.AgentId}|{h.MessageType}",
                h => h,
                StringComparer.OrdinalIgnoreCase);

        var sequencedHops = context.HopSequence
            .OrderBy(h => h.Step)
            .Select(h =>
            {
                string normalizedType = NormalizeToRequestType(h.MessageType);
                bool hasTelemetry = telemetryByAgentAndType.TryGetValue($"{h.FromAgent}|{normalizedType}", out AgentHopTelemetry? telemetry)
                    && string.Equals(h.Direction, "response", StringComparison.OrdinalIgnoreCase);

                return new
                {
                    step = h.Step,
                    from = h.FromAgent,
                    to = h.ToAgent,
                    direction = h.Direction,
                    messageType = h.MessageType,
                    correlationId = h.CorrelationId,
                    timestamp = h.Timestamp,
                    operation = hasTelemetry ? telemetry!.Operation : null,
                    functionsCalled = hasTelemetry ? telemetry!.FunctionsCalled : [],
                    toolsCalled = hasTelemetry ? telemetry!.ToolsCalled : [],
                    tokenUsage = hasTelemetry
                        ? new
                        {
                            inputTokens = telemetry!.MeasuredInputTokens,
                            outputTokens = telemetry!.MeasuredOutputTokens,
                            totalTokens = telemetry!.MeasuredTotalTokens,
                            exactUsageAvailable = telemetry!.ExactUsageAvailable,
                            responseId = telemetry!.ResponseId,
                            modelName = telemetry!.ModelName,
                            invocationCostUsd = telemetry!.CostUsd,
                            pricingTier = telemetry!.PricingTier
                        }
                        : null
                };
            });

        return new
        {
            response = responseText,
            agent = forcedAgent ?? context.CurrentRoute?.TargetAgent,
            conversationId = context.ConversationId,
            traceId = context.TraceId,
            turns = context.MessageHistory.Select(m => new
            {
                role = m.Role,
                content = m.Content,
                timestamp = m.Timestamp
            }),
            topics = topics.Concat(context.RoutedAgents).Distinct(StringComparer.OrdinalIgnoreCase),
            routing = new
            {
                primaryAgent = context.CurrentRoute?.TargetAgent,
                secondaryAgents = context.CurrentRoute?.SecondaryAgents ?? [],
                routedAgents = context.RoutedAgents.Distinct(StringComparer.OrdinalIgnoreCase),
                intentCategory = context.CurrentRoute?.IntentCategory,
                confidence = context.CurrentRoute?.Confidence
            },
            functionsCalled = context.FunctionsCalled.Distinct(StringComparer.OrdinalIgnoreCase),
            toolsCalled = context.ToolsCalled.Distinct(StringComparer.OrdinalIgnoreCase),
            agentHops = sequencedHops,
            tokenUsage = new
            {
                mode = context.TokenMeasurementMode.ToString(),
                exactUsageAvailable = totalTokens.ExactUsageAvailable,
                supervisor = context.SupervisorTokens != null ? new
                {
                    exactInputTokens = context.SupervisorTokens.ExactInputTokens,
                    exactOutputTokens = context.SupervisorTokens.ExactOutputTokens,
                    exactTotalTokens = context.SupervisorTokens.ExactTotalTokens,
                    estimatedInputTokens = context.SupervisorTokens.EstimatedInputTokens,
                    estimatedOutputTokens = context.SupervisorTokens.EstimatedOutputTokens,
                    estimatedTotalTokens = context.SupervisorTokens.EstimatedTotalTokens,
                    measuredInputTokens = context.SupervisorTokens.MeasuredInputTokens,
                    measuredOutputTokens = context.SupervisorTokens.MeasuredOutputTokens,
                    measuredTotalTokens = context.SupervisorTokens.MeasuredTotalTokens,
                    costUsd = context.SupervisorTokens.CostUsd,
                    isMeasuredExact = context.SupervisorTokens.IsMeasuredExact,
                    measuredSource = context.SupervisorTokens.MeasuredSource
                } : null,
                downstream = context.DownstreamTokens != null ? new
                {
                    exactInputTokens = context.DownstreamTokens.ExactInputTokens,
                    exactOutputTokens = context.DownstreamTokens.ExactOutputTokens,
                    exactTotalTokens = context.DownstreamTokens.ExactTotalTokens,
                    estimatedInputTokens = context.DownstreamTokens.EstimatedInputTokens,
                    estimatedOutputTokens = context.DownstreamTokens.EstimatedOutputTokens,
                    estimatedTotalTokens = context.DownstreamTokens.EstimatedTotalTokens,
                    measuredInputTokens = context.DownstreamTokens.MeasuredInputTokens,
                    measuredOutputTokens = context.DownstreamTokens.MeasuredOutputTokens,
                    measuredTotalTokens = context.DownstreamTokens.MeasuredTotalTokens,
                    costUsd = context.DownstreamTokens.CostUsd,
                    isMeasuredExact = context.DownstreamTokens.IsMeasuredExact,
                    measuredSource = context.DownstreamTokens.MeasuredSource
                } : null,
                total = new
                {
                    exactInputTokens = totalTokens.ExactInputTokens,
                    exactOutputTokens = totalTokens.ExactOutputTokens,
                    exactTotalTokens = totalTokens.ExactTotalTokens,
                    estimatedInputTokens = totalTokens.EstimatedInputTokens,
                    estimatedOutputTokens = totalTokens.EstimatedOutputTokens,
                    estimatedTotalTokens = totalTokens.EstimatedTotalTokens,
                    measuredInputTokens = totalTokens.MeasuredInputTokens,
                    measuredOutputTokens = totalTokens.MeasuredOutputTokens,
                    measuredTotalTokens = totalTokens.MeasuredTotalTokens,
                    costUsd = totalTokens.CostUsd,
                    isMeasuredExact = totalTokens.IsMeasuredExact,
                    measuredSource = totalTokens.MeasuredSource
                }
            }
        };
    }

    private static List<ConversationMessage> GetConversationHistory(string conversationId) => !s_conversationHistory.TryGetValue(conversationId, out List<ConversationMessage>? existing)
            ? []
            : existing
            .OrderBy(m => m.Timestamp)
            .Select(m => new ConversationMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToList();

    private static void SaveConversationHistory(string conversationId, IEnumerable<ConversationMessage> history)
    {
        var trimmed = history
            .OrderBy(m => m.Timestamp)
            .TakeLast(20)
            .Select(m => new ConversationMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            })
            .ToList();

        s_conversationHistory[conversationId] = trimmed;
    }

    private static TokenMeasurementMode ResolveTokenMeasurementMode(ChatRequest chatRequest)
    {
        var rawMode = chatRequest.TokenMeasurementMode;
        if (string.IsNullOrWhiteSpace(rawMode) && chatRequest.Metadata is { Count: > 0 })
        {
            rawMode = TryGetMetadataValue(chatRequest.Metadata, "tokenMeasurementMode")
                ?? TryGetMetadataValue(chatRequest.Metadata, "token_measurement_mode")
                ?? TryGetMetadataValue(chatRequest.Metadata, "tokenMode")
                ?? TryGetMetadataValue(chatRequest.Metadata, "token_mode");
        }

        if (string.IsNullOrWhiteSpace(rawMode))
        {
            return TokenMeasurementMode.Hybrid;
        }

        if (string.Equals(rawMode, "estimate", StringComparison.OrdinalIgnoreCase))
        {
            return TokenMeasurementMode.Estimated;
        }

        return Enum.TryParse<TokenMeasurementMode>(rawMode, ignoreCase: true, out var parsed)
            ? parsed
            : TokenMeasurementMode.Hybrid;
    }

    private static string? TryGetMetadataValue(Dictionary<string, object?> metadata, string key) =>
        metadata.TryGetValue(key, out var value) ? value?.ToString() : null;

    /// <summary>Inbound chat request DTO.</summary>
    private sealed record ChatRequest(
        string Message,
        string? UserId = null,
        string? ConversationId = null,
        string? TokenMeasurementMode = null,
        Dictionary<string, object?>? Metadata = null);
}
