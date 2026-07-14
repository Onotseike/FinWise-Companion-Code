namespace FinWise.MauiApp.Models;

/// <summary>
/// Intent routing information determined by the supervisor.
/// </summary>
public class IntentRoute
{
    public string TargetAgent { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string IntentCategory { get; set; } = string.Empty;
    public Dictionary<string, object?> ExtractedParameters { get; set; } = [];
    public string SpecialInstructions { get; set; } = string.Empty;
    public bool RequiresMultiAgentCoordination { get; set; }
    public List<string> SecondaryAgents { get; set; } = [];
}
