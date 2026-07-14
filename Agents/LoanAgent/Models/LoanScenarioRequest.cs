namespace FinWise.LoanAgent.Models;

/// <summary>
/// Request model for mortgage scenario evaluation endpoint.
/// </summary>
public class LoanScenarioRequest
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
    /// Property price for the scenario.
    /// </summary>
    public decimal PropertyPrice { get; set; }

    /// <summary>
    /// Down payment amount.
    /// </summary>
    public decimal DownPayment { get; set; }

    /// <summary>
    /// Loan term in years.
    /// </summary>
    public int LoanTermYears { get; set; }

    /// <summary>
    /// Interest rate percentage.
    /// </summary>
    public decimal InterestRate { get; set; }

    /// <summary>
    /// Optional metadata for tracking and analysis.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
