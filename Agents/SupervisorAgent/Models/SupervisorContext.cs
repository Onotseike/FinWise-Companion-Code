using FinWise.Shared.Core.AgentFramework;
using FinWise.Shared.Core.Telemetry;

namespace FinWise.SupervisorAgent.Models;

/// <summary>
/// Contains conversation context and routing information for the Supervisor Agent.
/// Extends BaseAgentContext with supervisor-specific routing and conversation management.
/// </summary>
public class SupervisorContext : BaseAgentContext
{
    /// <summary>Gets or sets the conversation history (for multi-turn conversations).</summary>
    public List<ConversationMessage> MessageHistory { get; set; } = [];

    /// <summary>Gets or sets the current intent route determined by the LLM.</summary>
    public IntentRoute? CurrentRoute { get; set; }

    /// <summary>
    /// Supervisor's token measurement (request processing, intent classification, etc.).
    /// </summary>
    public TokenMeasurement? SupervisorTokens { get; set; } = new();

    /// <summary>
    /// Aggregated token measurement for all downstream agents combined.
    /// </summary>
    public TokenMeasurement? DownstreamTokens { get; set; } = new();

    /// <summary>
    /// Total token measurement across supervisor and all downstream agents.
    /// </summary>
    public TokenMeasurement? TotalTokens { get; set; } = new();

    /// <summary>Gets or sets the token measurement mode used.</summary>
    public TokenMeasurementMode TokenMeasurementMode { get; set; } = TokenMeasurementMode.Hybrid;

    /// <summary>Gets or sets whether exact usage was available.</summary>
    public bool ExactUsageAvailable { get; set; }

    /// <summary>Gets or sets the duration in milliseconds.</summary>
    public long DurationMs { get; set; }

    /// <summary>Gets or sets retry count.</summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Legacy properties for backward compatibility.
    /// These are deprecated - use SupervisorTokens, DownstreamTokens, TotalTokens instead.
    /// </summary>
    [Obsolete("Use SupervisorTokens.EstimatedInputTokens instead", false)]
    public int EstimatedInputTokens 
    { 
        get => (int)(SupervisorTokens?.EstimatedInputTokens ?? 0);
        set 
        { 
            if (SupervisorTokens == null) SupervisorTokens = new();
            SupervisorTokens.EstimatedInputTokens = value;
        }
    }

    [Obsolete("Use SupervisorTokens.EstimatedOutputTokens instead", false)]
    public int EstimatedOutputTokens 
    { 
        get => (int)(SupervisorTokens?.EstimatedOutputTokens ?? 0);
        set 
        { 
            if (SupervisorTokens == null) SupervisorTokens = new();
            SupervisorTokens.EstimatedOutputTokens = value;
        }
    }

    [Obsolete("Use SupervisorTokens.EstimatedTotalTokens instead", false)]
    public int EstimatedTotalTokens 
    { 
        get => (int)(SupervisorTokens?.EstimatedTotalTokens ?? 0);
        set 
        { 
            if (SupervisorTokens == null) SupervisorTokens = new();
            SupervisorTokens.EstimatedTotalTokens = value;
        }
    }

    [Obsolete("Use SupervisorTokens.MeasuredInputTokens instead", false)]
    public int MeasuredInputTokens => (int)(SupervisorTokens?.MeasuredInputTokens ?? 0);

    [Obsolete("Use SupervisorTokens.MeasuredOutputTokens instead", false)]
    public int MeasuredOutputTokens => (int)(SupervisorTokens?.MeasuredOutputTokens ?? 0);

    [Obsolete("Use SupervisorTokens.MeasuredTotalTokens instead", false)]
    public int MeasuredTotalTokens => (int)(SupervisorTokens?.MeasuredTotalTokens ?? 0);

    [Obsolete("Use DownstreamTokens.MeasuredInputTokens instead", false)]
    public long DownstreamInputTokens 
    { 
        get => DownstreamTokens?.MeasuredInputTokens ?? 0;
        set
        {
            if (DownstreamTokens == null) DownstreamTokens = new();
            DownstreamTokens.EstimatedInputTokens = value;
            DownstreamTokens.ExactInputTokens = null;
        }
    }

    [Obsolete("Use DownstreamTokens.MeasuredOutputTokens instead", false)]
    public long DownstreamOutputTokens 
    { 
        get => DownstreamTokens?.MeasuredOutputTokens ?? 0;
        set
        {
            if (DownstreamTokens == null) DownstreamTokens = new();
            DownstreamTokens.EstimatedOutputTokens = value;
            DownstreamTokens.ExactOutputTokens = null;
        }
    }

    [Obsolete("Use DownstreamTokens.MeasuredTotalTokens instead", false)]
    public long DownstreamTotalTokens 
    { 
        get => DownstreamTokens?.MeasuredTotalTokens ?? 0;
        set
        {
            if (DownstreamTokens == null) DownstreamTokens = new();
            DownstreamTokens.EstimatedTotalTokens = value;
            DownstreamTokens.ExactTotalTokens = null;
        }
    }

    [Obsolete("Use DownstreamTokens.CostUsd instead", false)]
    public decimal DownstreamCostUsd 
    { 
        get => DownstreamTokens?.CostUsd ?? 0m;
        set
        {
            if (DownstreamTokens == null) DownstreamTokens = new();
            DownstreamTokens.CostUsd = value;
        }
    }

    /// <summary>Gets or sets the routed agents encountered while processing this request.</summary>
    public List<string> RoutedAgents { get; set; } = [];

    /// <summary>Gets or sets best-effort function names executed across hops.</summary>
    public List<string> FunctionsCalled { get; set; } = [];

    /// <summary>Gets or sets best-effort tool names executed across hops.</summary>
    public List<string> ToolsCalled { get; set; } = [];

    /// <summary>Gets or sets per-hop telemetry records for delegated agent calls.</summary>
    public List<AgentHopTelemetry> AgentHops { get; set; } = [];

    /// <summary>Gets or sets ordered hop transitions for the full request lifecycle.</summary>
    public List<AgentHopSequenceItem> HopSequence { get; set; } = [];

    /// <summary>Gets or sets an internal monotonic counter for hop ordering.</summary>
    public int HopSequenceCounter { get; set; }
}

/// <summary>
/// Ordered transition record for agent-to-agent request/response flow.
/// </summary>
public class AgentHopSequenceItem
{
    public int Step { get; set; }
    public string FromAgent { get; set; } = string.Empty;
    public string ToAgent { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty; // request|response
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-hop execution telemetry captured by Supervisor for delegated A2A calls.
/// Extends TokenMeasurement to add hop-specific metadata (agent, operation, functions, tools, cost).
/// </summary>
public class AgentHopTelemetry : TokenMeasurement
{
    public string AgentId { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string[] FunctionsCalled { get; set; } = [];
    public string[] ToolsCalled { get; set; } = [];
    public string? ResponseId { get; set; }
    public string? ModelName { get; set; }
    public string? PricingTier { get; set; }
}

/// <summary>
/// Represents a single message in the conversation history.
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the routing decision from LLM intent analysis.
/// Indicates which agent should handle the user's request and what context to pass.
/// </summary>
public class IntentRoute
{
    /// <summary>Gets or sets the target agent identifier (e.g., "budgeting", "loan",).</summary>
    public string TargetAgent { get; set; } = string.Empty;

    /// <summary>Gets or sets the confidence score of the routing decision (0-1).</summary>
    public decimal Confidence { get; set; }

    /// <summary>Gets or sets the specific intent category (e.g., "budget_creation", "loan_application", "suspicious_transaction").</summary>
    public string IntentCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets extracted parameters relevant to the target agent.</summary>
    public Dictionary<string, object?> ExtractedParameters { get; set; } = [];

    /// <summary>Gets or sets any special instructions or notes for the target agent.</summary>
    public string SpecialInstructions { get; set; } = string.Empty;

    /// <summary>Gets or sets a flag indicating if this request requires multi-agent coordination.</summary>
    public bool RequiresMultiAgentCoordination { get; set; }

    /// <summary>Gets or sets list of secondary agents to involve if multi-agent coordination is needed.</summary>
    public List<string> SecondaryAgents { get; set; } = [];
}

/// <summary>
/// Represents a request to delegate work to a specialist agent.
/// </summary>
public class AgentDelegationRequest
{
    public string TargetAgent { get; set; } = string.Empty;
    public string UserMessage { get; set; } = string.Empty;
    public SupervisorContext Context { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Represents the response from a delegated agent.
/// </summary>
public class AgentDelegationResponse
{
    public string SourceAgent { get; set; } = string.Empty;
    public string ResponseText { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object?> ResponseMetadata { get; set; } = [];
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Registry of available specialist agents and their capabilities.
/// </summary>
public sealed class AgentRegistry
{
    public static readonly AgentRegistry Instance = new();

    private static readonly Dictionary<string, AgentDescriptor> Agents = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "budgeting",
            new AgentDescriptor
            {
                Id = "budgeting",
                Name = "Budgeting Agent",
                Description = "Manages budget creation, tracking, analysis, and financial health assessment",
                Capabilities = ["budget_creation", "budget_analysis", "expense_tracking", "financial_health"],
                ManagementEndpoint = Environment.GetEnvironmentVariable("BUDGETING_AGENT_ENDPOINT") ?? "http://localhost:7071",
                ServiceBusTopic = "agent-messages"
            }
        },
        {
            "loan",
            new AgentDescriptor
            {
                Id = "loan",
                Name = "Loan Management Agent",
                Description = "Handles loan applications, mortgage calculations, and loan portfolio management",
                Capabilities = ["loan_application", "mortgage_calculation", "loan_analysis", "property_valuation"],
                ManagementEndpoint = Environment.GetEnvironmentVariable("LOAN_AGENT_ENDPOINT") ?? "http://localhost:7072",
                ServiceBusTopic = "agent-messages"
            }
        }
    };

    private AgentRegistry() { }

    /// <summary>Gets the descriptor for a specific agent.</summary>
    public static AgentDescriptor? GetAgent(string agentId) =>
        Agents.TryGetValue(agentId, out var agent) ? agent : null;

    /// <summary>Gets all registered agents.</summary>
    public static IEnumerable<AgentDescriptor> GetAllAgents() => Agents.Values;

    /// <summary>Determines if an agent is registered and available.</summary>
    public static bool IsAgentAvailable(string agentId) => Agents.ContainsKey(agentId);
}

/// <summary>
/// Metadata describing a specialist agent and its capabilities.
/// </summary>
public class AgentDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = [];
    public string ManagementEndpoint { get; set; } = string.Empty;
    public string ServiceBusTopic { get; set; } = string.Empty;
}
