namespace FinWise.MauiApp.Models;

/// <summary>
/// Wrapper class that associates a chat message with its corresponding telemetry data.
/// This allows us to retrieve telemetry when a user taps on a message.
/// </summary>
public class MessageWithTelemetry
{
    /// <summary>
    /// Unique identifier for this message telemetry record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string MessageText { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was sent/received.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Whether this is a user message (true) or assistant message (false).
    /// </summary>
    public bool IsUserMessage { get; set; }

    /// <summary>
    /// The full enhanced supervisor response containing telemetry for this message.
    /// Only populated for assistant messages.
    /// </summary>
    public EnhancedSupervisorResponse? Telemetry { get; set; }

    /// <summary>
    /// Summary of agent hops for quick display.
    /// </summary>
    public string AgentHopsSummary { get; set; } = string.Empty;

    /// <summary>
    /// Summary of functions called for quick display.
    /// </summary>
    public string FunctionsCalledSummary { get; set; } = string.Empty;

    /// <summary>
    /// Summary of tools called for quick display.
    /// </summary>
    public string ToolsCalledSummary { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID associated with this message.
    /// </summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// The agent that handled this message (if routed directly).
    /// </summary>
    public string? SelectedAgent { get; set; }
}
