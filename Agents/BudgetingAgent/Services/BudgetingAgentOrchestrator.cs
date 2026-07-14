using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using FinWise.Shared.Core;
using FinWise.Shared.Core.AgentFramework;
using FinWise.BudgetingAgent.Models;
using FinWise.BudgetingAgent.Plugins;

namespace FinWise.BudgetingAgent.Services;

/// <summary>
/// Modernized Budgeting Agent Orchestrator extending BaseAgent<T>.
/// Handles budget analysis, tracking, advice generation, and financial health assessments.
/// Delegates AI model invocation to FinancialsAIAgent to leverage token tracking and metrics.
/// </summary>
public class BudgetingAgentOrchestrator(
    AIAgent? agent,
    AgentOptions options,
    FinancialsAIAgent financialsAIAgent,
    BudgetingAgentStateHandler? stateHandler,
    ILogger<BudgetingAgentOrchestrator> logger,
    IServiceProvider services) : BaseAgent<BudgetingAgentContext>(agent, options, logger, services)
{
    private readonly FinancialsAIAgent _financialsAIAgent = financialsAIAgent ?? throw new ArgumentNullException(nameof(financialsAIAgent));
    private readonly BudgetingAgentStateHandler? _stateHandler = stateHandler;

    /// <summary>
    /// Analyzes financial health based on user query.
    /// </summary>
    public async Task<string> AnalyzeFinancialHealthAsync(
        BudgetingAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Analyzing financial health. ConversationId={ConversationId}, UserId={UserId}, Operation={Operation}",
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
            Logger.LogError(ex, "Error analyzing financial health for ConversationId={ConversationId}", context.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Provides personalized budget advice based on financial data.
    /// </summary>
    public async Task<string> ProvideBudgetAdviceAsync(
        BudgetingAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Providing budget advice. ConversationId={ConversationId}, UserId={UserId}",
            context.ConversationId, context.UserId);

        context.Operation = "provide_budget_advice";

        var prompt = """
        Analyze the user's current financial situation and provide comprehensive budget advice. Please:

        1. Consider the user's account summary, recent transactions, and spending patterns
        2. Estimate the user's rough monthly income from entries and categories
        3. Review existing budgets to see how they're performing
        4. Analyze expense categories to identify areas for optimization

        Based on this data, provide specific recommendations for:
        - Areas where spending could be reduced
        - Budget adjustments
        - Savings opportunities
        - Financial goals alignment

        Present the analysis in a clear, actionable format.
        """;

        context.UserMessage = prompt;
        return await AnalyzeFinancialHealthAsync(context, cancellationToken);
    }

    /// <summary>
    /// Analyzes spending patterns for a specified timeframe.
    /// </summary>
    public async Task<string> AnalyzeSpendingPatternsAsync(
        BudgetingAgentContext context,
        string timeframe,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Analyzing spending patterns. ConversationId={ConversationId}, Timeframe={Timeframe}",
            context.ConversationId, timeframe);

        context.Operation = "analyze_spending_patterns";
        context.Parameters["timeframe"] = timeframe;

        var prompt = $"""
        Analyze the user's spending and income strictly for this requested timeframe: {timeframe}.

        Rules:
        1. Use only entries inside the requested timeframe.
        2. Do not fallback to current month unless the timeframe is missing or invalid.
        3. If timeframe is unclear, state what date range you used.

        Please:
        1. Review transactions for the specified timeframe
        2. Break down expenses and income by category
        3. Identify trends and patterns within that same range only
        4. Highlight unusual or concerning spending behavior

        Provide insights about:
        - Top expense categories
        - Total income vs total expenses
        - Recurring expenses in the selected range
        - One-time large purchases
        - Potential areas for cost reduction

        Present the analysis in a clear, structured format with actionable recommendations.
        """;

        context.UserMessage = prompt;
        return await AnalyzeFinancialHealthAsync(context, cancellationToken);
    }

    /// <summary>
    /// Creates a personalized budget for the user.
    /// </summary>
    public async Task<string> CreatePersonalizedBudgetAsync(
        BudgetingAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Creating personalized budget. ConversationId={ConversationId}, UserId={UserId}",
            context.ConversationId, context.UserId);

        context.Operation = "create_personalized_budget";

        var prompt = """
        Create a personalized budget for the user based on their financial data. Please:

        1. Use the user's account summary and recent transaction history
        2. Estimate their rough monthly income from entries and category types
        3. Analyze their income sources and amounts
        4. Review their current spending patterns by category
        5. Check existing budgets
        6. Get their expense categories and typical amounts

        Create a comprehensive budget that includes:
        - Recommended budget amounts for each spending category
        - Income allocation suggestions (50/30/20 rule or similar)
        - Emergency fund recommendations
        - Savings goals based on their financial situation
        - Specific actionable steps to implement the budget

        Base the recommendations on their actual spending history and provide realistic, achievable targets.
        """;

        context.UserMessage = prompt;
        return await AnalyzeFinancialHealthAsync(context, cancellationToken);
    }

    /// <summary>
    /// Provides a brief, concise summary of the user's financial situation.
    /// Returns key metrics and insights in bullet-point format, not comprehensive analysis.
    /// </summary>
    public async Task<string> GetBudgetSummaryAsync(
        BudgetingAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Getting budget summary. ConversationId={ConversationId}, UserId={UserId}",
            context.ConversationId, context.UserId);

        context.Operation = "get_budget_summary";

        var prompt = """
        Provide a BRIEF summary of the user's financial situation in 3-5 bullet points. Keep it concise and actionable:

        1. Quick snapshot of their income vs expenses (monthly estimate)
        2. Top 2-3 spending categories
        3. One key insight or recommendation

        Use bullet points. Max 150 words. Do not provide comprehensive analysis or detailed breakdowns.
        Focus on what matters most for quick understanding.
        """;

        context.UserMessage = prompt;
        return await AnalyzeFinancialHealthAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: Provides budget advice without requiring full context.
    /// </summary>
    public async Task<string> ProvideBudgetAdviceAsync(CancellationToken cancellationToken = default)
    {
        var context = new BudgetingAgentContext { UserId = "a2a-user", ConversationId = Guid.NewGuid().ToString() };
        return await ProvideBudgetAdviceAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: Analyzes financial health with simple query string.
    /// </summary>
    public async Task<string> AnalyzeFinancialHealthAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        var context = new BudgetingAgentContext { UserId = "a2a-user", ConversationId = Guid.NewGuid().ToString(), UserMessage = userQuery };
        return await AnalyzeFinancialHealthAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: Creates personalized budget without requiring full context.
    /// </summary>
    public async Task<string> CreatePersonalizedBudgetAsync(CancellationToken cancellationToken = default)
    {
        var context = new BudgetingAgentContext { UserId = "a2a-user", ConversationId = Guid.NewGuid().ToString() };
        return await CreatePersonalizedBudgetAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: Gets brief budget summary without requiring full context.
    /// </summary>
    public async Task<string> GetBudgetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var context = new BudgetingAgentContext { UserId = "a2a-user", ConversationId = Guid.NewGuid().ToString() };
        return await GetBudgetSummaryAsync(context, cancellationToken);
    }

    /// <summary>
    /// A2A-compatible overload: Analyzes spending patterns with timeframe parameter.
    /// </summary>
    public async Task<string> AnalyzeSpendingPatternsAsync(string timeframe, CancellationToken cancellationToken = default)
    {
        var context = new BudgetingAgentContext { UserId = "a2a-user", ConversationId = Guid.NewGuid().ToString() };
        return await AnalyzeSpendingPatternsAsync(context, timeframe, cancellationToken);
    }

    /// <summary>
    /// Implements abstract method from BaseAgent to provide agent-specific invoke logic.
    /// Delegates AI model invocation to FinancialsAIAgent which handles:
    /// - Token tracking (exact vs estimated vs hybrid)
    /// - Retry logic with exponential backoff
    /// - Metrics emission to Application Insights
    /// </summary>
    protected override async Task<string> InvokeAgentAsync(
        string userMessage,
        BudgetingAgentContext context,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation(
            "Invoking FinancialsAIAgent via BudgetingAgentOrchestrator. ConversationId={ConversationId}, UserId={UserId}, Operation={Operation}",
            context.ConversationId, context.UserId, context.Operation);

        try
        {
            // Delegate to FinancialsAIAgent for AI model invocation with token tracking
            var response = await _financialsAIAgent.AnalyzeFinancialHealthAsync(
                userMessage,
                route: context.Operation ?? "orchestrator",
                experimentPhase: "baseline",
                scenario: context.Operation ?? "general",
                workflowId: context.ConversationId,
                hopId: $"budgeting-{context.UserId}"
            );

            // Extract and store token usage from response
            // UsageDetails contains InputTokenCount, OutputTokenCount, and TotalTokenCount
            var inputTokens = response.Usage?.InputTokenCount ?? 0;
            var outputTokens = response.Usage?.OutputTokenCount ?? 0;
            var totalTokens = response.Usage?.TotalTokenCount ?? (inputTokens + outputTokens);

            context.InputTokens = inputTokens;
            context.OutputTokens = outputTokens;
            context.TokensUsed = totalTokens;
            context.LastResponseId = response.ResponseId;
            context.LastInvokedAt = DateTime.UtcNow;

            Logger.LogInformation(
                "AI invocation completed. ResponseId={ResponseId}, MessageCount={MessageCount}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, TotalTokens={TotalTokens}",
                response.ResponseId, response.Messages.Count, inputTokens, outputTokens, totalTokens);

            return response.Text ?? "No response from AI agent";
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error invoking FinancialsAIAgent. ConversationId={ConversationId}, Operation={Operation}",
                context.ConversationId, context.Operation);
            throw;
        }
    }

    /// <summary>
    /// Implements abstract method to persist budgeting context to Cosmos DB.
    /// </summary>
    protected override async Task PersistStateAsync(BudgetingAgentContext context, CancellationToken cancellationToken = default)
    {
        if (_stateHandler != null)
        {
            var persisted = await _stateHandler.PersistContextAsync(context, cancellationToken);
            if (!persisted)
            {
                Logger.LogWarning(
                    "Failed to persist context for UserId={UserId}, ConversationId={ConversationId}",
                    context.UserId, context.ConversationId);
            }
        }
    }

    /// <summary>
    /// Implements abstract method to hydrate context from Cosmos DB.
    /// </summary>
    protected override async Task HydrateContextAsync(BudgetingAgentContext context, CancellationToken cancellationToken = default)
    {
        if (_stateHandler != null && context != null)
        {
            var hydrated = await _stateHandler.GetContextAsync(context.UserId, context.ConversationId);
            if (hydrated != null)
            {
                // Merge persisted state with current context
                if (CurrentContext is null)
                {
                    CurrentContext = hydrated;
                }
                else
                {
                    CurrentContext.AccountSummary = hydrated.AccountSummary ?? CurrentContext.AccountSummary;
                    CurrentContext.RecentTransactions = hydrated.RecentTransactions ?? CurrentContext.RecentTransactions;
                    CurrentContext.Budgets = hydrated.Budgets ?? CurrentContext.Budgets;
                    CurrentContext.ExpenseCategories = hydrated.ExpenseCategories ?? CurrentContext.ExpenseCategories;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the current context.
    /// </summary>
    private BudgetingAgentContext? CurrentContext { get; set; }
}
