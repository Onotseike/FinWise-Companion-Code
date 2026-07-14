namespace FinWise.LoanAgent.Models;

/// <summary>
/// Response model for mortgage analysis endpoint.
/// </summary>
public class LoanAnalysisResponse
{
    /// <summary>
    /// Conversation identifier for tracking.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Mortgage analysis results.
    /// </summary>
    public string Analysis { get; set; } = string.Empty;

    /// <summary>
    /// Trace identifier for distributed tracing.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Span identifier for distributed tracing.
    /// </summary>
    public string SpanId { get; set; } = string.Empty;
}
