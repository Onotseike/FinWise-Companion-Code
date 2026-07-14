namespace FinWise.MauiApp.Models;

/// <summary>
/// Comprehensive routing information from the supervisor,
/// including intent classification and agent selection logic.
/// </summary>
public class RoutingDetails
{
    /// <summary>
    /// Primary agent selected to handle the request.
    /// </summary>
    public string PrimaryAgent { get; set; } = string.Empty;

    /// <summary>
    /// Secondary agents that may assist (for multi-agent coordination).
    /// </summary>
    public List<string> SecondaryAgents { get; set; } = [];

    /// <summary>
    /// All agents involved in handling the request.
    /// </summary>
    public List<string> RoutedAgents { get; set; } = [];

    /// <summary>
    /// The intent category classified by the supervisor.
    /// Examples: "budget_analysis", "loan_comparison", "financial_planning"
    /// </summary>
    public string IntentCategory { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score of the routing decision (0.0 to 1.0).
    /// 1.0 = Direct routing (bypassed LLM classification)
    /// Lower values = LLM-based classification with uncertainty
    /// </summary>
    public decimal Confidence { get; set; } = 0m;

    /// <summary>
    /// Extracted parameters from user message relevant to the intent.
    /// </summary>
    public Dictionary<string, object?> ExtractedParameters { get; set; } = [];

    /// <summary>
    /// Special routing instructions for the target agent.
    /// </summary>
    public string SpecialInstructions { get; set; } = string.Empty;

    /// <summary>
    /// Whether this request requires coordination across multiple agents.
    /// </summary>
    public bool RequiresMultiAgentCoordination { get; set; }
}
