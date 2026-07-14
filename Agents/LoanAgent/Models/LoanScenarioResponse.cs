namespace FinWise.LoanAgent.Models;

/// <summary>
/// Response model for mortgage scenario evaluation endpoint.
/// </summary>
public class LoanScenarioResponse
{
    /// <summary>
    /// Conversation identifier for tracking.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Mortgage scenario evaluation results.
    /// </summary>
    public string Scenario { get; set; } = string.Empty;

    /// <summary>
    /// Trace identifier for distributed tracing.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;
}
