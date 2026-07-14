using System.Text.Json.Serialization;

namespace FinWise.Shared.Core.A2A;

/// <summary>
/// Standard message type constants for inter-agent communication between the three active agents:
/// SupervisorAgent, BudgetingAgent, and LoanAgent.
/// Format: {DomainAgent}.{Action} for requests, {DomainAgent}.{Action}.Result for responses
/// </summary>
public static class A2AMessageTypes
{
    // ==================== Financial Agent (Budgeting) ====================
    // Requests to Financial Agent
    public const string FinancialGetBudgetAdvice = "Financial.GetBudgetAdvice";
    public const string FinancialGetBudgetSummary = "Financial.GetBudgetSummary";
    public const string FinancialAnalyzeSpending = "Financial.AnalyzeSpending";
    public const string FinancialAnalyzeHealth = "Financial.AnalyzeHealth";
    public const string FinancialCreateBudget = "Financial.CreateBudget";

    // Responses from Financial Agent
    public const string FinancialGetBudgetAdviceResult = "Financial.GetBudgetAdvice.Result";
    public const string FinancialGetBudgetSummaryResult = "Financial.GetBudgetSummary.Result";
    public const string FinancialAnalyzeSpendingResult = "Financial.AnalyzeSpending.Result";
    public const string FinancialAnalyzeHealthResult = "Financial.AnalyzeHealth.Result";
    public const string FinancialCreateBudgetResult = "Financial.CreateBudget.Result";

    // ==================== Loan Agent ====================
    public const string LoanGetMortgageOptions = "Loan.GetMortgageOptions";
    public const string LoanCompareMortgages = "Loan.CompareMortgages";
    public const string LoanGetPropertyAdvice = "Loan.GetPropertyAdvice";

    public const string LoanGetMortgageOptionsResult = "Loan.GetMortgageOptions.Result";
    public const string LoanCompareMortgagesResult = "Loan.CompareMortgages.Result";
    public const string LoanGetPropertyAdviceResult = "Loan.GetPropertyAdvice.Result";
}

/// <summary>
/// Enhanced agent envelope for inter-agent communication with support for tracing and context.
/// </summary>
/// <typeparam name="TPayload">The payload type</typeparam>
public record AgentEnvelope<TPayload>(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("messageType")] string MessageType,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("sender")] string Sender,
    [property: JsonPropertyName("recipient")] string Recipient,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("payload")] TPayload Payload,
    [property: JsonPropertyName("traceId")] string? TraceId = null,
    [property: JsonPropertyName("spanId")] string? SpanId = null,
    [property: JsonPropertyName("timeoutMs")] int TimeoutMs = 30000,
    [property: JsonPropertyName("requestorContext")] Dictionary<string, object>? RequestorContext = null
)
{
    /// <summary>
    /// Creates an agent envelope with the standard message format.
    /// </summary>
    public static AgentEnvelope<TPayload> Create(
        string type,
        string sender,
        string recipient,
        string correlationId,
        TPayload payload,
        string? traceId = null,
        string? spanId = null,
        int timeoutMs = 30000) => new(
            "1.0",  // version
            type,  // messageType
            correlationId,  // correlation Id
            sender,  // sender
            recipient,  // recipient
            DateTime.UtcNow,  // timestamp
            payload,  // payload
            traceId ?? correlationId,  // traceId
            spanId ?? Guid.NewGuid().ToString("N")[..16],  // spanId
            timeoutMs,  // timeoutMs
            null);  // requestorContext

    /// <summary>
    /// Creates a response envelope from a request envelope.
    /// </summary>
    public static AgentEnvelope<TPayload> CreateResponse(
        AgentEnvelope<object> request,
        string resultMessageType,
        TPayload responsePayload,
        string responderId) => new(
            request.Version,  // version
            resultMessageType,  // messageType
            request.CorrelationId,  // correlationId
            responderId,  // sender
            request.Sender,  // recipient
            DateTime.UtcNow,  // timestamp
            responsePayload,  // payload
            request.TraceId,  // traceId
            Guid.NewGuid().ToString("N")[..16],  // spanId
            request.TimeoutMs,  // timeoutMs
            null);  // requestorContext
}

// ==================== Payload Types ====================

/// <summary>
/// Simple text payload for basic string responses.
/// </summary>
public record SimpleTextPayload(
    [property: JsonPropertyName("text")] string Text);

/// <summary>
/// Token and duration usage details for an agent invocation hop.
/// Supports both exact (from provider) and estimated (calculated) token counts.
/// </summary>
public record AgentTokenUsage(
    [property: JsonPropertyName("inputTokens")] long InputTokens,
    [property: JsonPropertyName("outputTokens")] long OutputTokens,
    [property: JsonPropertyName("totalTokens")] long TotalTokens,
    [property: JsonPropertyName("durationMs")] long DurationMs,
    [property: JsonPropertyName("exactUsageAvailable")] bool ExactUsageAvailable,
    [property: JsonPropertyName("responseId")] string? ResponseId = null,
    [property: JsonPropertyName("modelName")] string? ModelName = null,
    [property: JsonPropertyName("invocationCostUsd")] decimal? InvocationCostUsd = null,
    [property: JsonPropertyName("pricingTier")] string? PricingTier = null,
    // Enhanced dual-metric fields for hybrid token mode support
    [property: JsonPropertyName("exactInputTokens")] long? ExactInputTokens = null,
    [property: JsonPropertyName("exactOutputTokens")] long? ExactOutputTokens = null,
    [property: JsonPropertyName("estimatedInputTokens")] long EstimatedInputTokens = 0,
    [property: JsonPropertyName("estimatedOutputTokens")] long EstimatedOutputTokens = 0);

/// <summary>
/// Rich A2A execution payload with response text and telemetry emitted by specialist agents.
/// </summary>
public record AgentExecutionPayload(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("operation")] string Operation,
    [property: JsonPropertyName("functionsCalled")] string[] FunctionsCalled,
    [property: JsonPropertyName("toolsCalled")] string[] ToolsCalled,
    [property: JsonPropertyName("routedAgents")] string[] RoutedAgents,
    [property: JsonPropertyName("tokenUsage")] AgentTokenUsage TokenUsage);

/// <summary>
/// Financial analysis request arguments.
/// </summary>
public record FinancialAnalyzeSpendingArgs(
    [property: JsonPropertyName("timeframe")] string Timeframe,
    [property: JsonPropertyName("userId")] string? UserId = null,
    [property: JsonPropertyName("categories")] string[]? IncludeCategories = null);

/// <summary>
/// Financial health analysis response.
/// </summary>
public record FinancialHealthPayload(
    [property: JsonPropertyName("overallScore")] double OverallScore,
    [property: JsonPropertyName("strengths")] string[] Strengths,
    [property: JsonPropertyName("improvements")] string[] Improvements,
    [property: JsonPropertyName("recommendation")] string Recommendation);

/// <summary>
/// Budget advice response.
/// </summary>
public record BudgetAdvicePayload(
    [property: JsonPropertyName("monthlyBudget")] decimal MonthlyBudget,
    [property: JsonPropertyName("recommendations")] string[] Recommendations,
    [property: JsonPropertyName("savingsOpportunities")] string[] SavingsOpportunities);

/// <summary>
/// Loan analysis request arguments sent from SupervisorAgent to LoanAgent.
/// Contains the user's message and optional pre-fetched financial context from BudgetingAgent.
/// </summary>
public record LoanAnalysisArgs(
    [property: JsonPropertyName("userMessage")] string UserMessage,
    [property: JsonPropertyName("financialContext")] LoanFinancialContext? FinancialContext = null,
    [property: JsonPropertyName("timeframe")] string? Timeframe = null,
    [property: JsonPropertyName("userId")] string? UserId = null);

/// <summary>
/// Snapshot of the user's financial context forwarded by SupervisorAgent to LoanAgent.
/// Mirrors the fields of LoanAgent.Models.FinancialContext so the Shared library
/// does not take a dependency on the agent project.
/// </summary>
public record LoanFinancialContext(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("monthlyIncome")] decimal MonthlyIncome,
    [property: JsonPropertyName("monthlyExpenses")] decimal MonthlyExpenses,
    [property: JsonPropertyName("availableSavings")] decimal AvailableSavings,
    [property: JsonPropertyName("debtCommitments")] decimal DebtCommitments,
    [property: JsonPropertyName("creditScore")] int CreditScore,
    [property: JsonPropertyName("budgetAdvice")] Dictionary<string, object?>? BudgetAdvice = null,
    [property: JsonPropertyName("spendingAnalysis")] Dictionary<string, object?>? SpendingAnalysis = null);

/// <summary>
/// Generic error payload for failed agent operations.
/// </summary>
public record ErrorPayload(
    [property: JsonPropertyName("errorCode")] string ErrorCode,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] string? Details = null,
    [property: JsonPropertyName("isRetryable")] bool IsRetryable = false);