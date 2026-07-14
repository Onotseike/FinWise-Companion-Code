namespace FinWise.MauiApp.Models;

/// <summary>
/// Telemetry for a single hop (message exchange) between agents in the request processing chain.
/// </summary>
public class AgentHopRecord
{
    /// <summary>
    /// Sequence number of this hop (1-based).
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Agent that initiated this hop.
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Agent that received this hop.
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Direction of the hop: "request" or "response".
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Type of message: e.g., "BudgetAnalysisRequest", "BudgetAnalysisRequest.Result".
    /// </summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID linking related hops.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the hop occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Name of the operation executed (if any).
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// List of functions called during this hop.
    /// </summary>
    public List<string> FunctionsCalled { get; set; } = [];

    /// <summary>
    /// List of tools (e.g., API calls) invoked during this hop.
    /// </summary>
    public List<string> ToolsCalled { get; set; } = [];

    /// <summary>
    /// Token usage metrics for this hop (if available).
    /// </summary>
    public TokenUsageInfo? TokenUsage { get; set; }
}
