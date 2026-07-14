namespace FinWise.MauiApp.Models;

/// <summary>
/// Request model for direct agent routing through the supervisor.
/// Routes to a specific agent bypassing automatic intent classification.
/// </summary>
public class SupervisorRouteRequest
{
    /// <summary>
    /// Conversation identifier for multi-turn conversations.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// User identifier for tracking and personalization.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The user's message to send to the target agent.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Requested token measurement mode: hybrid, exact, or estimated.
    /// </summary>
    public string? TokenMeasurementMode { get; set; }

    /// <summary>
    /// Optional metadata for tracking and telemetry.
    /// </summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}
