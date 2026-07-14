namespace FinWise.MauiApp.Models;

/// <summary>
/// Token usage information from Supervisor responses.
/// Includes exact (from provider), estimated (calculated), and measured (based on mode) token counts.
/// </summary>
public class TokenUsageInfo
{
    // Exact token counts from provider (Azure OpenAI)
    public int? ExactInputTokens { get; set; }
    public int? ExactOutputTokens { get; set; }
    public int? ExactTotalTokens { get; set; }

    // Estimated token counts (calculated using text.Length / 4.0)
    public int EstimatedInputTokens { get; set; }
    public int EstimatedOutputTokens { get; set; }
    public int EstimatedTotalTokens { get; set; }

    // Measured tokens (based on TokenMeasurementMode: exact, estimated, or hybrid)
    public int MeasuredInputTokens { get; set; }
    public int MeasuredOutputTokens { get; set; }
    public int MeasuredTotalTokens { get; set; }

    // Cost information
    public decimal? EstimatedCostUsd { get; set; }

    /// <summary>
    /// Token measurement mode used: "exact", "estimated", or "hybrid"
    /// </summary>
    public string TokenMeasurementMode { get; set; } = "hybrid";

    /// <summary>
    /// Whether exact usage data was available from the provider.
    /// </summary>
    public bool ExactUsageAvailable { get; set; }

    // Response metadata
    public string? ResponseId { get; set; }
    public long DurationMs { get; set; }
    public int RetryCount { get; set; }
}
