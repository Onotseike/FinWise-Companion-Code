namespace FinWise.LoanAgent.Models;

/// <summary>
/// Request model for mortgage analysis endpoint.
/// </summary>
public class LoanAnalysisRequest
{
    /// <summary>
    /// User identifier (optional, defaults to "anonymous").
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Conversation identifier (optional, generated if not provided).
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// User message or query for mortgage analysis.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional metadata for tracking and analysis.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
