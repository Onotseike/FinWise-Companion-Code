using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure;
using System.Diagnostics;

namespace FinWise.BudgetingAgent.Services;

public class FinancialsAIAgent(AIAgent agent, IConfiguration configuration, ILogger<FinancialsAIAgent> logger)
{
    private readonly AIAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<FinancialsAIAgent> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const int MaxRetries = 3;

    public string AgentId => _configuration["Values:AGENT_ID"] ?? _configuration["AGENT_ID"] ?? "BudgetingAgent";
    public string TokenMeasurementMode => NormalizeTokenMode(
        (_configuration["Values:TokenMeasurementMode"]
         ?? _configuration["TokenMeasurementMode"]
         ?? "hybrid").Trim().ToLowerInvariant());
    private const int BaseDelayMs = 1000;

    public async Task<AgentResponse> AnalyzeFinancialHealthAsync(
        string userQuery,
        string route = "unknown",
        string experimentPhase = "baseline",
        string scenario = "default",
        string? workflowId = null,
        string? hopId = null)
    {
        int retryCount = 0;
        long startedAtTicks = Stopwatch.GetTimestamp();
        int estimatedInputTokens = EstimateTokenCount(userQuery);
        var deploymentName = _configuration["Values:AzureOpenAIChatDeploymentName"]
                             ?? _configuration["AzureOpenAIChatDeploymentName"]
                             ?? _configuration["Values:AzureOpenAIDeploymentName"]
                             ?? _configuration["AzureOpenAIDeploymentName"]
                             ?? "unknown";
        var endpoint = _configuration["Values:AzureOpenAIEndpoint"]
                       ?? _configuration["AzureOpenAIEndpoint"]
                       ?? "unknown";

        _logger.LogInformation(
            "Starting FinancialsAIAgent run. AgentId={AgentId}, WorkflowId={WorkflowId}, HopId={HopId}, Route={Route}, Scenario={Scenario}, ExperimentPhase={ExperimentPhase}, Deployment={DeploymentName}, Endpoint={Endpoint}, PromptLength={PromptLength}, EstimatedInputTokens={EstimatedInputTokens}",
            AgentId,
            workflowId,
            hopId,
            route,
            scenario,
            experimentPhase,
            deploymentName,
            endpoint,
            userQuery.Length,
            estimatedInputTokens);

        while (true)
        {
            try
            {
                var response = await _agent.RunAsync(userQuery);
                var usage = response.Usage;
                bool isUsageAvailable = usage is not null;
                int? exactInputTokens = (int?)usage?.InputTokenCount;
                int? exactOutputTokens = (int?)usage?.OutputTokenCount;
                int? exactTotalTokens = (int?)usage?.TotalTokenCount ?? (exactInputTokens + exactOutputTokens);
                int estimatedOutputTokens = EstimateTokenCount(response.Text);
                int estimatedTotalTokens = estimatedInputTokens + estimatedOutputTokens;
                int measuredInputTokens = ResolveMeasuredTokens(estimatedInputTokens, exactInputTokens);
                int measuredOutputTokens = ResolveMeasuredTokens(estimatedOutputTokens, exactOutputTokens);
                int measuredTotalTokens = ResolveMeasuredTokens(estimatedTotalTokens, exactTotalTokens, measuredInputTokens + measuredOutputTokens);
                long elapsedMs = GetElapsedMilliseconds(startedAtTicks);

                if (TokenMeasurementMode == "exact" && !isUsageAvailable)
                {
                    _logger.LogWarning(
                        "Exact token mode requested but provider usage was unavailable. Falling back to estimated tokens. AgentId={AgentId}, Route={Route}, Scenario={Scenario}, ResponseId={ResponseId}",
                        AgentId,
                        route,
                        scenario,
                        response.ResponseId);
                }

                _logger.LogInformation(
                    "FinancialsAIAgent run completed. AgentId={AgentId}, SdkAgentInstanceId={SdkAgentInstanceId}, Deployment={DeploymentName}, ResponseId={ResponseId}, MessageCount={MessageCount}, TextLength={TextLength}",
                    AgentId,
                    response.AgentId,
                    deploymentName,
                    response.ResponseId,
                    response.Messages.Count,
                    response.Text.Length);

                _logger.LogInformation(
                    "AiRunMetrics AgentId={AgentId}, WorkflowId={WorkflowId}, HopId={HopId}, Route={Route}, Scenario={Scenario}, ExperimentPhase={ExperimentPhase}, Deployment={DeploymentName}, ResponseId={ResponseId}, DurationMs={DurationMs}, RetryCount={RetryCount}, TokenMeasurementMode={TokenMeasurementMode}, ExactUsageAvailable={ExactUsageAvailable}, ExactUsageSource={ExactUsageSource}, MeasuredInputTokens={MeasuredInputTokens}, MeasuredOutputTokens={MeasuredOutputTokens}, MeasuredTotalTokens={MeasuredTotalTokens}, ExactInputTokens={ExactInputTokens}, ExactOutputTokens={ExactOutputTokens}, ExactTotalTokens={ExactTotalTokens}, EstimatedInputTokens={EstimatedInputTokens}, EstimatedOutputTokens={EstimatedOutputTokens}, EstimatedTotalTokens={EstimatedTotalTokens}, Status={Status}",
                    AgentId,
                    workflowId,
                    hopId,
                    route,
                    scenario,
                    experimentPhase,
                    deploymentName,
                    response.ResponseId,
                    elapsedMs,
                    retryCount,
                    TokenMeasurementMode,
                    isUsageAvailable,
                    "AgentResponse.Usage",
                    measuredInputTokens,
                    measuredOutputTokens,
                    measuredTotalTokens,
                    exactInputTokens,
                    exactOutputTokens,
                    exactTotalTokens,
                    estimatedInputTokens,
                    estimatedOutputTokens,
                    estimatedTotalTokens,
                    "success");

                if (string.IsNullOrWhiteSpace(response.Text))
                {
                    _logger.LogWarning(
                        "FinancialsAIAgent returned empty text. AgentId={AgentId}, SdkAgentInstanceId={SdkAgentInstanceId}, Deployment={DeploymentName}, ResponseId={ResponseId}, MessageCount={MessageCount}",
                        AgentId,
                        response.AgentId,
                        deploymentName,
                        response.ResponseId,
                        response.Messages.Count);
                }

                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 429 && retryCount < MaxRetries)
            {
                retryCount++;
                var delay = CalculateExponentialBackoff(retryCount);
                _logger.LogWarning(
                    ex,
                    "FinancialsAIAgent throttled by Azure OpenAI. Deployment={DeploymentName}, RetryAttempt={RetryAttempt}, DelayMs={DelayMs}",
                    deploymentName,
                    retryCount,
                    delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
            catch (Exception ex) when (ex.Message.Contains("429") && retryCount < MaxRetries)
            {
                retryCount++;
                var delay = CalculateExponentialBackoff(retryCount);
                _logger.LogWarning(
                    ex,
                    "FinancialsAIAgent encountered 429-compatible exception. Deployment={DeploymentName}, RetryAttempt={RetryAttempt}, DelayMs={DelayMs}",
                    deploymentName,
                    retryCount,
                    delay.TotalMilliseconds);
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                long elapsedMs = GetElapsedMilliseconds(startedAtTicks);
                _logger.LogError(
                    ex,
                    "FinancialsAIAgent run failed. Deployment={DeploymentName}, Endpoint={Endpoint}, PromptLength={PromptLength}, ExceptionType={ExceptionType}",
                    deploymentName,
                    endpoint,
                    userQuery.Length,
                    ex.GetType().FullName);

                _logger.LogWarning(
                    "AiRunMetrics AgentId={AgentId}, WorkflowId={WorkflowId}, HopId={HopId}, Route={Route}, Scenario={Scenario}, ExperimentPhase={ExperimentPhase}, Deployment={DeploymentName}, DurationMs={DurationMs}, RetryCount={RetryCount}, TokenMeasurementMode={TokenMeasurementMode}, ExactUsageAvailable={ExactUsageAvailable}, ExactUsageSource={ExactUsageSource}, MeasuredInputTokens={MeasuredInputTokens}, MeasuredOutputTokens={MeasuredOutputTokens}, MeasuredTotalTokens={MeasuredTotalTokens}, ExactInputTokens={ExactInputTokens}, ExactOutputTokens={ExactOutputTokens}, ExactTotalTokens={ExactTotalTokens}, EstimatedInputTokens={EstimatedInputTokens}, EstimatedOutputTokens={EstimatedOutputTokens}, EstimatedTotalTokens={EstimatedTotalTokens}, Status={Status}, ExceptionType={ExceptionType}",
                    AgentId,
                    workflowId,
                    hopId,
                    route,
                    scenario,
                    experimentPhase,
                    deploymentName,
                    elapsedMs,
                    retryCount,
                    TokenMeasurementMode,
                    false,
                    "none",
                    estimatedInputTokens,
                    0,
                    estimatedInputTokens,
                    null,
                    null,
                    null,
                    estimatedInputTokens,
                    0,
                    estimatedInputTokens,
                    "failed",
                    ex.GetType().Name);
                throw;
            }
        }
    }

    private static TimeSpan CalculateExponentialBackoff(int retryAttempt)
    {
        var delayMs = BaseDelayMs * Math.Pow(2, retryAttempt - 1);
        var jitter = Random.Shared.Next(0, 200);
        return TimeSpan.FromMilliseconds(delayMs + jitter);
    }

    private static long GetElapsedMilliseconds(long startedAtTicks)
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - startedAtTicks;
        return (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
    }

    // Lightweight token estimate to compare relative changes across experiments.
    private static int EstimateTokenCount(string? text) => string.IsNullOrWhiteSpace(text) ? 0 : (int)Math.Ceiling(text.Length / 4.0);

    private int ResolveMeasuredTokens(int estimated, int? exact, int? fallback = null) => TokenMeasurementMode switch
    {
        "exact" => exact ?? fallback ?? estimated,
        "estimate" => estimated,
        "estimated" => estimated,
        _ => exact ?? fallback ?? estimated
    };

    private static string NormalizeTokenMode(string mode) =>
        mode switch
        {
            "estimate" => "estimated",
            "exact" => "exact",
            _ => "hybrid"
        };

    public async Task<AgentResponse> ProvideBudgetAdviceAsync(
        string route = "ai/budget-advice",
        string experimentPhase = "baseline",
        string scenario = "budget-advice",
        string? workflowId = null,
        string? hopId = null)
    {
        string prompt = """
                    Analyze the user's current financial situation and provide comprehensive budget advice. Please:

                    1. Get the user's account summary for the current month
                    2. Retrieve recent transactions to understand spending patterns
                    3. Check existing budgets to see how they're performing
                    4. Look at expense categories to identify areas for optimization

                    Based on this data, provide specific recommendations for:
                    - Areas where spending could be reduced
                    - Budget adjustments
                    - Savings opportunities
                    - Financial goals alignment

                    Present the analysis in a clear, actionable format.
                    """;

        AgentResponse response = await AnalyzeFinancialHealthAsync(prompt, route, experimentPhase, scenario, workflowId, hopId);
        return response;
    }

    public async Task<AgentResponse> AnalyzeSpendingPatternsAsync(
        string timeframe,
        string route = "ai/analyze-spending",
        string experimentPhase = "baseline",
        string scenario = "analyze-spending",
        string? workflowId = null,
        string? hopId = null)
    {
        string prompt = $"""
                     Analyze the user's spending patterns for the {timeframe}. Please:

                     1. Get recent transactions for the specified timeframe
                     2. Categorize spending by type
                     3. Identify trends and patterns
                     4. Compare against previous periods if data is available
                     5. Look for unusual or concerning spending behavior

                     Provide insights about:
                     - Top spending categories
                     - Recurring expenses
                     - One-time large purchases
                     - Potential areas for cost reduction
                     - Spending trends over time

                     Present the analysis in a clear, structured format with actionable recommendations.
                     """;

        return await AnalyzeFinancialHealthAsync(prompt, route, experimentPhase, scenario, workflowId, hopId);
    }

    public async Task<AgentResponse> CreatePersonalizedBudgetAsync(
        string route = "ai/create-budget",
        string experimentPhase = "baseline",
        string scenario = "create-budget",
        string? workflowId = null,
        string? hopId = null)
    {
        string prompt = """
                    Create a personalized budget for the user based on their financial data. Please:

                    1. Get the user's account summary and recent transaction history
                    2. Analyze their income sources and amounts
                    3. Review their current spending patterns by category
                    4. Check existing budgets if any
                    5. Get their expense categories and typical amounts

                    Create a comprehensive budget that includes:
                    - Recommended budget amounts for each spending category
                    - Income allocation suggestions (50/30/20 rule or similar)
                    - Emergency fund recommendations
                    - Savings goals based on their financial situation
                    - Specific actionable steps to implement the budget

                    Base the recommendations on their actual spending history and provide realistic, achievable targets.
                    """;
        return await AnalyzeFinancialHealthAsync(prompt, route, experimentPhase, scenario, workflowId, hopId);
    }
}