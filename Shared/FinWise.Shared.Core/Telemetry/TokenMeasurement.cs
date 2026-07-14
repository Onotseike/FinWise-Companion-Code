
using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.Telemetry;
/// <summary>
/// Measurement mode for token counting: exact from provider, estimated calculation, or hybrid (prefer exact, fallback to estimate).
/// </summary>
[JsonConverter(typeof(TokenMeasurementModeJsonConverter))]
public enum TokenMeasurementMode
{
    /// <summary>Use only exact tokens from model provider (e.g., Azure OpenAI usage_completion_tokens).</summary>
    Exact = 0,

    /// <summary>Use only estimated tokens (calculated as text.Length / 4.0).</summary>
    Estimated = 1,

    /// <summary>Prefer exact tokens if available from provider, fallback to estimated calculation.</summary>
    Hybrid = 2
}

/// <summary>
/// Unified token measurement model with exact, estimated, and measured tokens based on measurement mode.
/// Used consistently across supervisor and all downstream agents.
/// </summary>
public class TokenMeasurement
{
    /// <summary>
    /// Exact input tokens from the model provider (e.g., Azure OpenAI).
    /// Null if not available from provider.
    /// </summary>
    public long? ExactInputTokens { get; set; }

    /// <summary>
    /// Exact output tokens from the model provider.
    /// Null if not available from provider.
    /// </summary>
    public long? ExactOutputTokens { get; set; }

    /// <summary>
    /// Exact total tokens from the model provider (input + output).
    /// Null if not available from provider.
    /// </summary>
    public long? ExactTotalTokens { get; set; }

    /// <summary>
    /// Estimated input tokens (calculated as text.Length / 4.0).
    /// Always computed for fallback purposes.
    /// </summary>
    public long EstimatedInputTokens { get; set; }

    /// <summary>
    /// Estimated output tokens (calculated as text.Length / 4.0).
    /// Always computed for fallback purposes.
    /// </summary>
    public long EstimatedOutputTokens { get; set; }

    /// <summary>
    /// Estimated total tokens (input + output).
    /// Always computed for fallback purposes.
    /// </summary>
    public long EstimatedTotalTokens { get; set; }

    /// <summary>
    /// The measurement mode used to compute Measured* values.
    /// </summary>
    public TokenMeasurementMode Mode { get; set; } = TokenMeasurementMode.Hybrid;

    /// <summary>
    /// Whether exact usage data is available from the model provider.
    /// Determines whether Measured* uses exact or estimated values.
    /// </summary>
    public bool ExactUsageAvailable { get; set; }

    /// <summary>
    /// Measured input tokens based on the measurement mode.
    /// - Exact mode: ExactInputTokens ?? 0
    /// - Estimated mode: EstimatedInputTokens
    /// - Hybrid mode: ExactInputTokens ?? EstimatedInputTokens
    /// </summary>
    public long MeasuredInputTokens => Mode switch
    {
        TokenMeasurementMode.Exact => ExactInputTokens ?? 0,
        TokenMeasurementMode.Estimated => EstimatedInputTokens,
        TokenMeasurementMode.Hybrid => ExactInputTokens ?? EstimatedInputTokens,
        _ => EstimatedInputTokens
    };

    /// <summary>
    /// Measured output tokens based on the measurement mode.
    /// </summary>
    public long MeasuredOutputTokens => Mode switch
    {
        TokenMeasurementMode.Exact => ExactOutputTokens ?? 0,
        TokenMeasurementMode.Estimated => EstimatedOutputTokens,
        TokenMeasurementMode.Hybrid => ExactOutputTokens ?? EstimatedOutputTokens,
        _ => EstimatedOutputTokens
    };

    /// <summary>
    /// Measured total tokens based on the measurement mode.
    /// </summary>
    public long MeasuredTotalTokens => Mode switch
    {
        TokenMeasurementMode.Exact => ExactTotalTokens ?? 0,
        TokenMeasurementMode.Estimated => EstimatedTotalTokens,
        TokenMeasurementMode.Hybrid => ExactTotalTokens ?? EstimatedTotalTokens,
        _ => EstimatedTotalTokens
    };

    /// <summary>
    /// Cost of this measurement in USD (calculated from measured tokens).
    /// </summary>
    public decimal CostUsd { get; set; }

    /// <summary>
    /// Whether the measured tokens are exact (true) or estimated (false).
    /// Useful for UI to show confidence indicator or measurement source.
    /// </summary>
    public bool IsMeasuredExact => Mode == TokenMeasurementMode.Exact || (Mode == TokenMeasurementMode.Hybrid && ExactUsageAvailable);

    /// <summary>
    /// Human-readable measurement source for UI/logging: "exact", "estimated", or "hybrid (exact)" / "hybrid (estimated)".
    /// </summary>
    public string MeasuredSource => Mode switch
    {
        TokenMeasurementMode.Exact => "exact",
        TokenMeasurementMode.Estimated => "estimated",
        TokenMeasurementMode.Hybrid => ExactUsageAvailable ? "hybrid (exact)" : "hybrid (estimated)",
        _ => "unknown"
    };

    /// <summary>
    /// Creates a token measurement from exact and estimated token counts.
    /// Automatically determines IsMeasuredExact based on mode and availability.
    /// </summary>
    public static TokenMeasurement Create(
        long? exactInputTokens,
        long? exactOutputTokens,
        long estimatedInputTokens,
        long estimatedOutputTokens,
        TokenMeasurementMode mode = TokenMeasurementMode.Hybrid,
        decimal costUsd = 0m)
    {
        var exactTotal = (exactInputTokens ?? 0) + (exactOutputTokens ?? 0);
        var estimatedTotal = estimatedInputTokens + estimatedOutputTokens;

        return new TokenMeasurement
        {
            ExactInputTokens = exactInputTokens,
            ExactOutputTokens = exactOutputTokens,
            ExactTotalTokens = exactTotal > 0 ? exactTotal : null,
            EstimatedInputTokens = estimatedInputTokens,
            EstimatedOutputTokens = estimatedOutputTokens,
            EstimatedTotalTokens = estimatedTotal,
            Mode = mode,
            ExactUsageAvailable = exactInputTokens.HasValue || exactOutputTokens.HasValue,
            CostUsd = costUsd
        };
    }

    /// <summary>
    /// Aggregates multiple token measurements into a single combined measurement.
    /// Useful for combining supervisor + downstream metrics.
    /// </summary>
    public static TokenMeasurement Aggregate(params TokenMeasurement[] measurements)
    {
        if (measurements == null || measurements.Length == 0)
        {
            return new TokenMeasurement();
        }

        var distinctModes = measurements.Select(m => m.Mode).Distinct().ToArray();
        var aggregateMode = distinctModes.Length == 1 ? distinctModes[0] : TokenMeasurementMode.Hybrid;
        var totalExactInput = measurements.Sum(m => m.ExactInputTokens ?? 0);
        var totalExactOutput = measurements.Sum(m => m.ExactOutputTokens ?? 0);
        var totalEstimatedInput = measurements.Sum(m => m.EstimatedInputTokens);
        var totalEstimatedOutput = measurements.Sum(m => m.EstimatedOutputTokens);
        var totalCost = measurements.Sum(m => m.CostUsd);

        var allExactAvailable = measurements.All(m => m.ExactUsageAvailable);

        return new TokenMeasurement
        {
            ExactInputTokens = totalExactInput > 0 ? totalExactInput : null,
            ExactOutputTokens = totalExactOutput > 0 ? totalExactOutput : null,
            ExactTotalTokens = (totalExactInput + totalExactOutput) > 0 ? totalExactInput + totalExactOutput : null,
            EstimatedInputTokens = totalEstimatedInput,
            EstimatedOutputTokens = totalEstimatedOutput,
            EstimatedTotalTokens = totalEstimatedInput + totalEstimatedOutput,
            Mode = aggregateMode,
            ExactUsageAvailable = allExactAvailable,
            CostUsd = totalCost
        };
    }
}

/// <summary>
/// Aggregated token metrics across supervisor and all downstream agents.
/// Provides holistic view of token usage for the entire request.
/// </summary>
public class AggregatedTokenMetrics
{
    /// <summary>
    /// Token measurement for the supervisor's processing.
    /// </summary>
    public TokenMeasurement? Supervisor { get; set; }

    /// <summary>
    /// Aggregated token measurement for all downstream agents combined.
    /// </summary>
    public TokenMeasurement? Downstream { get; set; }

    /// <summary>
    /// Combined token measurement across supervisor and all downstream agents.
    /// </summary>
    public TokenMeasurement? Total { get; set; }

    /// <summary>
    /// Measurement mode used across all agents.
    /// </summary>
    public TokenMeasurementMode Mode { get; set; } = TokenMeasurementMode.Hybrid;

    /// <summary>
    /// Whether exact usage data is available across all agents.
    /// </summary>
    public bool ExactUsageAvailable { get; set; }
}
