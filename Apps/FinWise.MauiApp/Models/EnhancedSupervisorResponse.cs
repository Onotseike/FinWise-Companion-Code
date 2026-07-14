namespace FinWise.MauiApp.Models;

/// <summary>
/// Enhanced response from the supervisor that includes full telemetry data.
/// This model captures the rich payload emitted by the supervisor endpoint.
/// </summary>
public class EnhancedSupervisorResponse
{
    /// <summary>
    /// The response text from the supervisor or routed agent.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// The primary agent that handled this request.
    /// </summary>
    public string? Agent { get; set; }

    /// <summary>
    /// Conversation ID for multi-turn tracking.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Complete conversation history as a list of turns.
    /// </summary>
    public List<ConversationTurn> Turns { get; set; } = [];

    /// <summary>
    /// Topics/categories covered in this request and response.
    /// </summary>
    public List<string> Topics { get; set; } = [];

    /// <summary>
    /// Detailed routing information for this request.
    /// </summary>
    public RoutingDetails? Routing { get; set; }

    /// <summary>
    /// All functions called across the supervisor and downstream agents.
    /// </summary>
    public List<string> FunctionsCalled { get; set; } = [];

    /// <summary>
    /// All tools (external APIs, services) invoked.
    /// </summary>
    public List<string> ToolsCalled { get; set; } = [];

    /// <summary>
    /// Individual agent hops showing the request/response flow with telemetry.
    /// Each hop includes operation, functions called, tools invoked, and token usage metrics.
    /// </summary>
    public List<AgentHopRecord> AgentHops { get; set; } = [];

    /// <summary>
    /// Complete token usage telemetry.
    /// </summary>
    public EnhancedTokenUsageTelemetry? TokenUsage { get; set; }

    /// <summary>
    /// Trace ID for debugging and observability.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;
}
