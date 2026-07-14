namespace FinWise.MauiApp.Models;

/// <summary>
/// Health check response from supervisor.
/// </summary>
public class SupervisorHealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
