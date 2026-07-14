namespace FinWise.MauiApp.Models;

// ==================== Request/Response Models ====================

/// <summary>
/// Request model for single-turn chat with the supervisor.
/// </summary>
public class SupervisorChatRequest
{
    public string? ConversationId { get; set; }
    public string? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TokenMeasurementMode { get; set; }
    public Dictionary<string, object?>? Metadata { get; set; }
}
