namespace FinWise.MauiApp.Models;

/// <summary>
/// Conversation message for multi-turn conversations.
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
