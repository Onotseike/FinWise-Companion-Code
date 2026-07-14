namespace FinWise.Shared.Core.Pricing;

/// <summary>
/// Represents a usage and cost report for a user or session.
/// Aggregates token consumption and costs across multiple invocations.
/// </summary>
public class UsageReport
{
    /// <summary>Gets or sets the user ID for this report.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the report period start date.</summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>Gets or sets the report period end date.</summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>Gets or sets total input tokens consumed.</summary>
    public long TotalInputTokens { get; set; }

    /// <summary>Gets or sets total output tokens consumed.</summary>
    public long TotalOutputTokens { get; set; }

    /// <summary>Gets or sets total tokens (input + output).</summary>
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;

    /// <summary>Gets or sets total USD cost for this period.</summary>
    public decimal TotalCostUsd { get; set; }

    /// <summary>Gets or sets number of AI invocations.</summary>
    public int InvocationCount { get; set; }

    /// <summary>Gets or sets average cost per invocation.</summary>
    public decimal AverageCostPerInvocation => InvocationCount > 0 ? TotalCostUsd / InvocationCount : 0;

    /// <summary>Gets or sets average tokens per invocation.</summary>
    public long AverageTokensPerInvocation => InvocationCount > 0 ? TotalTokens / InvocationCount : 0;

    /// <summary>Gets or sets breakdown by agent.</summary>
    public Dictionary<string, AgentUsage> ByAgent { get; set; } = [];

    /// <summary>Gets or sets breakdown by operation type.</summary>
    public Dictionary<string, OperationUsage> ByOperation { get; set; } = [];

    /// <summary>Gets or sets breakdown by pricing tier.</summary>
    public Dictionary<string, PricingTierUsage> ByPricingTier { get; set; } = [];

    /// <summary>
    /// Returns a human-readable summary.
    /// </summary>
    public override string ToString()
    {
        return $"UserId={UserId}, Period={PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}, " +
               $"Tokens={TotalTokens:N0}, Cost=${TotalCostUsd:F2}, Invocations={InvocationCount}";
    }
}

/// <summary>
/// Usage breakdown for a specific agent.
/// </summary>
public class AgentUsage
{
    /// <summary>Gets or sets the agent name (e.g., "budgeting", "loan").</summary>
    public string AgentName { get; set; } = string.Empty;

    /// <summary>Gets or sets total input tokens for this agent.</summary>
    public long InputTokens { get; set; }

    /// <summary>Gets or sets total output tokens for this agent.</summary>
    public long OutputTokens { get; set; }

    /// <summary>Gets or sets total tokens for this agent.</summary>
    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>Gets or sets total USD cost for this agent.</summary>
    public decimal CostUsd { get; set; }

    /// <summary>Gets or sets number of invocations of this agent.</summary>
    public int InvocationCount { get; set; }

    /// <summary>Gets or sets percentage of total cost (0-100).</summary>
    public decimal CostPercentage { get; set; }

    /// <summary>Gets or sets percentage of total tokens (0-100).</summary>
    public decimal TokenPercentage { get; set; }
}

/// <summary>
/// Usage breakdown for a specific operation type.
/// </summary>
public class OperationUsage
{
    /// <summary>Gets or sets the operation name (e.g., "provide_budget_advice", "analyze_mortgage").</summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>Gets or sets total tokens for this operation.</summary>
    public long TotalTokens { get; set; }

    /// <summary>Gets or sets total USD cost for this operation.</summary>
    public decimal CostUsd { get; set; }

    /// <summary>Gets or sets number of times this operation was invoked.</summary>
    public int InvocationCount { get; set; }

    /// <summary>Gets or sets average cost per invocation.</summary>
    public decimal AverageCostPerInvocation => InvocationCount > 0 ? CostUsd / InvocationCount : 0;

    /// <summary>Gets or sets average tokens per invocation.</summary>
    public long AverageTokensPerInvocation => InvocationCount > 0 ? TotalTokens / InvocationCount : 0;
}

/// <summary>
/// Usage breakdown for a specific pricing tier.
/// </summary>
public class PricingTierUsage
{
    /// <summary>Gets or sets the pricing tier identifier (e.g., "0-10000000").</summary>
    public string TierIdentifier { get; set; } = string.Empty;

    /// <summary>Gets or sets total tokens that fell into this tier.</summary>
    public long TotalTokens { get; set; }

    /// <summary>Gets or sets total USD cost for tokens in this tier.</summary>
    public decimal CostUsd { get; set; }

    /// <summary>Gets or sets percentage of costs in this tier (0-100).</summary>
    public decimal CostPercentage { get; set; }

    /// <summary>Gets or sets the effective input token rate (USD per 1K tokens) for this tier.</summary>
    public decimal InputTokenRatePer1k { get; set; }

    /// <summary>Gets or sets the effective output token rate (USD per 1K tokens) for this tier.</summary>
    public decimal OutputTokenRatePer1k { get; set; }
}

/// <summary>
/// Usage alert/threshold for cost management.
/// </summary>
public class UsageAlert
{
    /// <summary>Gets or sets the alert type (e.g., "daily_cost_exceeded", "token_quota_warning").</summary>
    public string AlertType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity (info, warning, critical).</summary>
    public string Severity { get; set; } = "warning";

    /// <summary>Gets or sets the alert message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the current value that triggered the alert.</summary>
    public double CurrentValue { get; set; }

    /// <summary>Gets or sets the threshold that was exceeded.</summary>
    public double Threshold { get; set; }

    /// <summary>Gets or sets when the alert was triggered.</summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets recommended actions.</summary>
    public List<string> RecommendedActions { get; set; } = [];
}

/// <summary>
/// Cost projection based on historical usage.
/// </summary>
public class CostProjection
{
    /// <summary>Gets or sets the user ID for this projection.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the projection date.</summary>
    public DateTime ProjectionDate { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets historical daily average cost (USD).</summary>
    public decimal DailyAverageCostUsd { get; set; }

    /// <summary>Gets or sets projected 30-day cost (USD).</summary>
    public decimal ProjectedMonthlyCostUsd => DailyAverageCostUsd * 30;

    /// <summary>Gets or sets projected annual cost (USD).</summary>
    public decimal ProjectedAnnualCostUsd => DailyAverageCostUsd * 365;

    /// <summary>Gets or sets the period this projection is based on.</summary>
    public int DaysOfHistoricalData { get; set; }

    /// <summary>Gets or sets confidence level (0-100%).</summary>
    /// <remarks>Lower if historical data is limited (&lt;7 days), higher with more data (&gt;30 days).</remarks>
    public int ConfidencePercentage { get; set; }

    /// <summary>Gets or sets trend indicator: "increasing", "stable", "decreasing".</summary>
    public string Trend { get; set; } = "stable";

    /// <summary>Gets or sets recommendations for cost optimization.</summary>
    public List<string> OptimizationRecommendations { get; set; } = [];
}
