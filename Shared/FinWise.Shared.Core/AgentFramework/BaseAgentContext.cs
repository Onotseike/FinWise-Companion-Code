namespace FinWise.Shared.Core.AgentFramework;

/// <summary>
/// Base context for all agents. Provides common properties for token tracking,
/// tracing, and operation metadata. All agent contexts should inherit from this class
/// to ensure consistent handling of cross-cutting concerns.
/// </summary>
public abstract class BaseAgentContext
{
    /// <summary>Gets or sets the user ID.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the conversation/session ID.</summary>
    public string ConversationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Gets or sets the current user message or query.</summary>
    public string UserMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested operation (e.g., "analyze_budget", "process_loan", etc.).</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Gets or sets operation-specific parameters as key-value pairs.</summary>
    public Dictionary<string, object?> Parameters { get; set; } = [];

    /// <summary>Gets or sets trace ID for distributed tracing across the system.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Gets or sets span ID for the current operation (correlates with trace ID).</summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>Gets or sets metadata about the context (arbitrary key-value pairs).</summary>
    public Dictionary<string, object?> Metadata { get; set; } = [];

    // Token accounting and metering properties
    /// <summary>Gets or sets the total tokens used in the last AI invocation.</summary>
    public long? TokensUsed { get; set; }

    /// <summary>Gets or sets the input (prompt) tokens from the last AI invocation, if available from provider.</summary>
    public long? InputTokens { get; set; }

    /// <summary>Gets or sets the output (completion) tokens from the last AI invocation, if available from provider.</summary>
    public long? OutputTokens { get; set; }

    /// <summary>Gets or sets the timestamp (UTC) of the last AI invocation.</summary>
    public DateTime? LastInvokedAt { get; set; }

    /// <summary>Gets or sets the response ID from the last AI invocation for distributed tracing and auditing.</summary>
    public string? LastResponseId { get; set; }

    // Cost tracking and billing properties
    /// <summary>Gets or sets the USD cost of the last AI invocation based on token usage.</summary>
    public decimal? LastInvocationCostUsd { get; set; }

    /// <summary>Gets or sets the cumulative USD cost across all AI invocations in this session.</summary>
    public decimal CumulativeSessionCostUsd { get; set; }

    /// <summary>Gets or sets the pricing tier that was applied to the last invocation.</summary>
    public string? LastPricingTier { get; set; }

    /// <summary>Gets or sets the model name that was used for the last invocation.</summary>
    public string? LastModelName { get; set; }

    /// <summary>Gets or sets when the last cost calculation was performed.</summary>
    public DateTime? CostCalculatedAt { get; set; }
}
