using FinWise.LoanAgent.Models;
using FinWise.Shared.Core;
using FinWise.Shared.Core.AgentFramework;
using FinWise.Shared.Core.Pricing;

using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinWise.LoanAgent.Services;

/// <summary>
/// Modernized Loan Agent Orchestrator extending BaseAgent<T>.
/// Handles mortgage analysis, loan scenarios, property evaluation, and recommendations.
/// Delegates AI model invocation to LoanFinancialsAIAgent for token tracking and metrics.
/// </summary>
public class LoanAgentOrchestrator(
    AIAgent? agent,
    AgentOptions options,
    LoanFinancialsAIAgent loanFinancialsAIAgent,
    LoanAgentStateHandler? stateHandler,
    ILogger<LoanAgentOrchestrator> logger,
    IServiceProvider services) : BaseAgent<LoanAgentContext>(agent, options, logger, services)
{
    private readonly LoanFinancialsAIAgent _loanFinancialsAIAgent = loanFinancialsAIAgent ?? throw new ArgumentNullException(nameof(loanFinancialsAIAgent));
    private readonly LoanAgentStateHandler? _stateHandler = stateHandler;

    /// <summary>
    /// Analyzes mortgage options based on user query and financial context.
    /// </summary>
    public async Task<string> AnalyzeMortgageOptionsAsync(
        LoanAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Analyzing mortgage options. ConversationId={ConversationId}, UserId={UserId}, Operation={Operation}",
            context.ConversationId, context.UserId, context.Operation);

        try
        {
            var result = await InvokeWithContextAsync(
                context.UserMessage,
                context,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error analyzing mortgage options for ConversationId={ConversationId}", context.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Provides comprehensive mortgage advice enriched with financial context from budgeting agent.
    /// </summary>
    public async Task<string> ProvideComprehensiveMortgageAdviceAsync(
        LoanAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Providing comprehensive mortgage advice. ConversationId={ConversationId}, UserId={UserId}",
            context.ConversationId, context.UserId);

        context.Operation = "comprehensive_mortgage_advice";

        var prompt = """
        You are a mortgage advisor AI assistant. Analyze the user's mortgage request considering their financial profile.

        ### User Query
        {UserMessage}

        ### Financial Context (from Budgeting Agent)
        Monthly Income: {MonthlyIncome}
        Monthly Expenses: {MonthlyExpenses}
        Available Savings: {AvailableSavings}
        Debt Commitments: {DebtCommitments}
        Credit Score: {CreditScore}
        Budgeting Summary: {BudgetingSummary}
        Spending Summary: {SpendingSummary}

        ### Your Task
        1. Assess mortgage affordability based on their financial situation
        2. Recommend appropriate down payment amounts
        3. Suggest suitable loan terms and types (fixed vs variable)
        4. Highlight any financial risks or considerations
        5. Integrate their spending patterns into recommendations

        If some fields are incomplete, use the budgeting and spending summaries to infer reasonable estimates and state assumptions.
        Do not ask for monthly income again when financial context already includes estimates or summary analysis.

        Provide clear, actionable mortgage advice that aligns with their financial health.
        """;

        var financialContext = context.FinancialContext ?? new FinancialContext();
        string budgetingSummary = financialContext.BudgetAdvice.TryGetValue("summary", out var summary)
            ? summary?.ToString() ?? "Not provided"
            : "Not provided";
        string spendingSummary = financialContext.SpendingAnalysis.TryGetValue("summary", out var spending)
            ? spending?.ToString() ?? "Not provided"
            : "Not provided";

        var enrichedPrompt = prompt
            .Replace("{UserMessage}", context.UserMessage)
            .Replace("{MonthlyIncome}", financialContext.MonthlyIncome.ToString("C"))
            .Replace("{MonthlyExpenses}", financialContext.MonthlyExpenses.ToString("C"))
            .Replace("{AvailableSavings}", financialContext.AvailableSavings.ToString("C"))
            .Replace("{DebtCommitments}", financialContext.DebtCommitments.ToString("C"))
            .Replace("{CreditScore}", financialContext.CreditScore.ToString())
            .Replace("{BudgetingSummary}", budgetingSummary)
            .Replace("{SpendingSummary}", spendingSummary);

        context.UserMessage = enrichedPrompt;
        return await AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// Evaluates a specific loan scenario with calculations.
    /// </summary>
    public async Task<string> EvaluateLoanScenarioAsync(
        LoanAgentContext context,
        decimal propertyPrice,
        decimal downPaymentPercent,
        int loanTermMonths,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Evaluating loan scenario. ConversationId={ConversationId}, PropertyPrice={PropertyPrice}, DownPayment={DownPayment}%",
            context.ConversationId, propertyPrice, downPaymentPercent);

        context.Operation = "evaluate_loan_scenario";
        context.Parameters["propertyPrice"] = propertyPrice;
        context.Parameters["downPaymentPercent"] = downPaymentPercent;
        context.Parameters["loanTermMonths"] = loanTermMonths;

        var prompt = $"""
        Please evaluate this loan scenario:
        
        Property Price: ${propertyPrice:N2}
        Down Payment: {downPaymentPercent}% (${propertyPrice * (downPaymentPercent / 100):N2})
        Loan Amount: ${propertyPrice * (1 - downPaymentPercent / 100):N2}
        Loan Term: {loanTermMonths} months ({loanTermMonths / 12} years)

        Considering the user's financial profile, calculate:
        1. Estimated monthly payment (principal & interest)
        2. Total interest cost
        3. Debt-to-income ratio impact
        4. Affordability assessment
        5. Risk factors and considerations

        Provide recommendations on whether this scenario is suitable.
        """;

        context.UserMessage = prompt;
        return await AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// Generates a property search recommendation.
    /// </summary>
    public async Task<string> RecommendPropertySearchAsync(
        LoanAgentContext context,
        string country,
        string? location,
        decimal minPrice,
        decimal maxPrice,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Recommending property search. ConversationId={ConversationId}, Country={Country}, PriceRange=${MinPrice}-${MaxPrice}",
            context.ConversationId, country, minPrice, maxPrice);

        context.Operation = "recommend_property_search";
        context.Parameters["country"] = country;
        context.Parameters["location"] = location ?? "any";
        context.Parameters["minPrice"] = minPrice;
        context.Parameters["maxPrice"] = maxPrice;

        var prompt = $"""
        Based on the user's financial profile and needs, recommend properties for mortgage consideration.

        Search Criteria:
        - Country: {country}
        - Location: {(string.IsNullOrEmpty(location) ? "any" : location)}
        - Budget: ${minPrice:N2} - ${maxPrice:N2}

        Financial Profile:
        - Monthly Income: {(context.FinancialContext?.MonthlyIncome ?? 0):C}
        - Monthly Expenses: {(context.FinancialContext?.MonthlyExpenses ?? 0):C}
        - Available for Down Payment: {(context.FinancialContext?.AvailableSavings ?? 0):C}

        Provide property recommendations that align with:
        1. Their financial capacity
        2. Estimated mortgage affordability
        3. Long-term financial goals
        4. Risk tolerance
        """;

        context.UserMessage = prompt;
        return await AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    // ============================================================
    // A2A overloads — called by LoanA2AFunctions (Service Bus trigger)
    // These accept simple string inputs so the trigger handler can
    // route to the correct method based on A2AMessageTypes.
    // ============================================================

    /// <summary>
    /// A2A-compatible overload: analyzes mortgage options from a plain user message.
    /// Builds a minimal <see cref="LoanAgentContext"/> internally so the trigger
    /// handler does not need to construct one.
    /// </summary>
    public Task<string> AnalyzeMortgageOptionsAsync(
        string userMessage,
        FinancialContext? financialContext = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new LoanAgentContext
        {
            UserId = userId ?? "a2a-user",
            ConversationId = Guid.NewGuid().ToString("N"),
            UserMessage = userMessage,
            Operation = "analyze_mortgage_options",
            FinancialContext = financialContext
        };
        return AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: provides comprehensive mortgage advice with pre-fetched
    /// financial context supplied by the SupervisorAgent.
    /// </summary>
    public Task<string> ProvideComprehensiveMortgageAdviceAsync(
        string userMessage,
        FinancialContext? financialContext = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new LoanAgentContext
        {
            UserId = userId ?? "a2a-user",
            ConversationId = Guid.NewGuid().ToString("N"),
            UserMessage = userMessage,
            Operation = "comprehensive_mortgage_advice",
            FinancialContext = financialContext
        };
        return ProvideComprehensiveMortgageAdviceAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: compares mortgage products for a user message.
    /// </summary>
    public Task<string> CompareMortgagesAsync(
        string userMessage,
        FinancialContext? financialContext = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new LoanAgentContext
        {
            UserId = userId ?? "a2a-user",
            ConversationId = Guid.NewGuid().ToString("N"),
            UserMessage = userMessage,
            Operation = "compare_mortgages",
            FinancialContext = financialContext
        };
        return AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: provides property investment and purchasing advice.
    /// </summary>
    public Task<string> GetPropertyAdviceAsync(
        string userMessage,
        FinancialContext? financialContext = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new LoanAgentContext
        {
            UserId = userId ?? "a2a-user",
            ConversationId = Guid.NewGuid().ToString("N"),
            UserMessage = userMessage,
            Operation = "property_advice",
            FinancialContext = financialContext
        };
        return AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// Implements abstract method from BaseAgent to provide agent-specific invoke logic.
    /// Delegates AI model invocation to LoanFinancialsAIAgent which handles:
    /// - Token tracking (exact vs estimated vs hybrid)
    /// - Retry logic with exponential backoff
    /// - Metrics emission to Application Insights
    /// </summary>
    protected override async Task<string> InvokeAgentAsync(
        string userMessage,
        LoanAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Invoking LoanFinancialsAIAgent via LoanAgentOrchestrator. ConversationId={ConversationId}, UserId={UserId}, Operation={Operation}",
            context.ConversationId, context.UserId, context.Operation);

        try
        {
            // Delegate to LoanFinancialsAIAgent for AI model invocation with token tracking
            var response = await _loanFinancialsAIAgent.AnalyzeLoanAsync(
                userMessage,
                route: context.Operation ?? "orchestrator",
                experimentPhase: "baseline",
                scenario: context.Operation ?? "general",
                workflowId: context.ConversationId,
                hopId: $"loan-{context.UserId}"
            );

            // Extract and store token usage from response
            // UsageDetails contains InputTokenCount, OutputTokenCount, and TotalTokenCount
            var responseUsage = response.Usage;
            var inputTokens = responseUsage?.InputTokenCount ?? 0;
            var outputTokens = responseUsage?.OutputTokenCount ?? 0;
            var totalTokens = responseUsage?.TotalTokenCount ?? (inputTokens + outputTokens);

            context.InputTokens = inputTokens;
            context.OutputTokens = outputTokens;
            context.TokensUsed = totalTokens;
            context.LastResponseId = response.ResponseId;
            context.LastInvokedAt = DateTime.UtcNow;

            // Calculate USD cost using token cost calculator
            try
            {
                var modelName = Services.GetService(typeof(IConfiguration)) is IConfiguration config
                    ? (config["Values:AzureOpenAIChatDeploymentName"] ?? config["AzureOpenAIChatDeploymentName"] ?? "gpt-5-nano")
                    : "gpt-5-nano";

                var costCalculator = new TokenCostCalculator(modelName);
                var costResult = costCalculator.CalculateCost(inputTokens, outputTokens);

                context.LastInvocationCostUsd = costResult.TotalCostUsd;
                context.LastModelName = costResult.ModelName;
                context.LastPricingTier = costResult.PricingTier;
                context.CostCalculatedAt = DateTime.UtcNow;
                context.CumulativeSessionCostUsd += costResult.TotalCostUsd;

                var totalCost = costResult.TotalCostUsd;
                var inputCost = costResult.InputCostUsd;
                var outputCost = costResult.OutputCostUsd;
                var sessionCost = context.CumulativeSessionCostUsd;
                var pricingTier = costResult.PricingTier;

                Logger.LogInformation(
                    "Cost calculation completed. Cost={Cost:F6} USD, InputCost={InputCost:F6}, OutputCost={OutputCost:F6}, CumulativeSessionCost={SessionCost:F6} USD, PricingTier={Tier}",
                    totalCost, inputCost, outputCost, sessionCost, pricingTier);
            }
            catch (Exception costEx)
            {
                Logger.LogWarning(
                    costEx,
                    "Failed to calculate cost. Will continue without cost tracking. ConversationId={ConversationId}",
                    context.ConversationId);
            }

            var responseId = response.ResponseId;
            var responseMessages = response.Messages;
            var messageCount = responseMessages.Count;
            var lastInvocationCost = context.LastInvocationCostUsd;
            var invocationCost = lastInvocationCost ?? 0;

            Logger.LogInformation(
                "AI invocation completed. ResponseId={ResponseId}, MessageCount={MessageCount}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, TotalTokens={TotalTokens}, InvocationCost={InvocationCost:F6} USD",
                responseId, messageCount, inputTokens, outputTokens, totalTokens, invocationCost);

            var responseText = response.Text;
            return responseText ?? "No response from AI agent";
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error invoking LoanFinancialsAIAgent. ConversationId={ConversationId}, Operation={Operation}",
                context.ConversationId, context.Operation);
            throw;
        }
    }

    /// <summary>
    /// Implements abstract method to persist loan context to Cosmos DB.
    /// </summary>
    protected override async Task PersistStateAsync(LoanAgentContext context, CancellationToken cancellationToken = default)
    {
        if (context != null && _stateHandler is not null)
        {
            _ = await _stateHandler.PersistContextAsync(context, cancellationToken);
        }
    }

    /// <summary>
    /// Implements abstract method to hydrate context from Cosmos DB.
    /// </summary>
    protected override async Task HydrateContextAsync(LoanAgentContext context, CancellationToken cancellationToken = default)
    {
        if (context != null && _stateHandler is not null)
        {
            var hydrated = await _stateHandler.GetContextAsync(context.UserId, context.ConversationId, cancellationToken);
            if (hydrated != null)
            {
                // Merge persisted state with current context
                context.Properties = hydrated.Properties;
                context.LoanScenarios = hydrated.LoanScenarios;
                context.Recommendations = hydrated.Recommendations;
                context.FinancialContext = hydrated.FinancialContext;
            }
        }
    }

    /// <summary>
    /// Evaluates a specific mortgage scenario with detailed financial analysis.
    /// </summary>
    public async Task<string> EvaluateMortgageScenarioAsync(
        LoanAgentContext context,
        LoanScenarioRequest scenarioRequest,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Evaluating mortgage scenario. ConversationId={ConversationId}, UserId={UserId}, PropertyPrice={PropertyPrice}",
            context.ConversationId, context.UserId, scenarioRequest.PropertyPrice);

        context.Operation = "evaluate_mortgage_scenario";

        var prompt = $"""
        You are a mortgage advisor. Analyze the following mortgage scenario in detail:

        ### Property Details
        Property Price: ${scenarioRequest.PropertyPrice:N2}
        Down Payment: ${scenarioRequest.DownPayment:N2}
        Down Payment %: {(scenarioRequest.DownPayment / scenarioRequest.PropertyPrice * 100):F1}%

        ### Loan Details
        Loan Amount: ${(scenarioRequest.PropertyPrice - scenarioRequest.DownPayment):N2}
        Loan Term: {scenarioRequest.LoanTermYears} years
        Interest Rate: {scenarioRequest.InterestRate:F2}%

        ### Financial Profile
        Monthly Income: {(context.FinancialContext?.MonthlyIncome ?? 0):C}
        Monthly Expenses: {(context.FinancialContext?.MonthlyExpenses ?? 0):C}
        Available Savings: {(context.FinancialContext?.AvailableSavings ?? 0):C}
        Credit Score: {(context.FinancialContext?.CreditScore ?? 0)}

        ### Analysis Required
        1. Calculate estimated monthly payment (P&I, taxes, insurance)
        2. Assess debt-to-income ratio impact
        3. Evaluate affordability based on financial profile
        4. Identify potential risks or concerns
        5. Provide recommendations (approve, consider alternatives, or decline)
        6. Suggest possible modifications to improve scenario viability

        Provide a comprehensive analysis with clear recommendations.
        """;

        context.UserMessage = prompt;
        return await AnalyzeMortgageOptionsAsync(context, cancellationToken);
    }

    /// <summary>
    /// Gets or sets the current context.
    /// </summary>
    private LoanAgentContext? CurrentContext { get; set; }
}
