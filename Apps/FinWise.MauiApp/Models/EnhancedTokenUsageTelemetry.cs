using FinWise.Shared.Core.Telemetry;

namespace FinWise.MauiApp.Models;

/// <summary>
/// Complete token usage telemetry with supervisor, downstream, and aggregate metrics.
/// Uses shared TokenMeasurement model for consistency across all agents.
/// </summary>
public class EnhancedTokenUsageTelemetry
{
    /// <summary>
    /// Supervisor's token measurement (request processing, intent classification, etc.).
    /// </summary>
    public TokenMeasurement? Supervisor { get; set; }

    /// <summary>
    /// Aggregated token measurement for all downstream agents combined.
    /// </summary>
    public TokenMeasurement? Downstream { get; set; }

    /// <summary>
    /// Total token measurement across supervisor and all downstream agents.
    /// </summary>
    public TokenMeasurement? Total { get; set; }

    /// <summary>
    /// Measurement mode used across all agents: Exact, Estimated, or Hybrid.
    /// </summary>
    public TokenMeasurementMode Mode { get; set; } = TokenMeasurementMode.Hybrid;

    /// <summary>
    /// Whether exact usage data is available from the model provider.
    /// </summary>
    public bool ExactUsageAvailable { get; set; }
}
