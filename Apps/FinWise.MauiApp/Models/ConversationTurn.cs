namespace FinWise.MauiApp.Models;

/// <summary>
/// Represents a single turn (message exchange) in the conversation.
/// </summary>
public class ConversationTurn
{
    /// <summary>
    /// Role of the sender: "user" or "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
