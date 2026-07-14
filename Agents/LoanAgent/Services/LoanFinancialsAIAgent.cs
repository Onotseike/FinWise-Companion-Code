using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure;
using System.Diagnostics;

namespace FinWise.LoanAgent.Services;

/// <summary>
/// Infrastructure layer for LoanAgent that wraps AIAgent with token tracking, retry logic, and metrics.
/// Handles loan-specific AI invocations with exponential backoff for rate limiting.
/// </summary>
public class LoanFinancialsAIAgent(AIAgent agent, IConfiguration configuration, ILogger<LoanFinancialsAIAgent> logger)
{
    private readonly AIAgent _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly ILogger<LoanFinancialsAIAgent> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const int MaxRetries = 3;

    public string AgentId => _configuration["Values:AGENT_ID"] ?? _configuration["AGENT_ID"] ?? "LoanAgent";
    public string TokenMeasurementMode => NormalizeTokenMode(
        (_configuration["Values:TokenMeasurementMode"]
         ?? _configuration["TokenMeasurementMode"]
         ?? "hybrid").Trim().ToLowerInvariant());
    private const int BaseDelayMs = 1000;

    /// <summary>
    /// Main entry point for loan-related AI invocations.
    /// </summary>
    public async Task<AgentResponse> AnalyzeLoanAsync(
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
            "Starting LoanFinancialsAIAgent run. AgentId={AgentId}, WorkflowId={WorkflowId}, HopId={HopId}, Route={Route}, Scenario={Scenario}, ExperimentPhase={ExperimentPhase}, Deployment={DeploymentName}, Endpoint={Endpoint}, PromptLength={PromptLength}, EstimatedInputTokens={EstimatedInputTokens}",
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
                    "LoanFinancialsAIAgent run completed. AgentId={AgentId}, SdkAgentInstanceId={SdkAgentInstanceId}, Deployment={DeploymentName}, ResponseId={ResponseId}, MessageCount={MessageCount}, TextLength={TextLength}",
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
                        "LoanFinancialsAIAgent returned empty text. AgentId={AgentId}, SdkAgentInstanceId={SdkAgentInstanceId}, Deployment={DeploymentName}, ResponseId={ResponseId}, MessageCount={MessageCount}",
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
                    "LoanFinancialsAIAgent throttled by Azure OpenAI. Deployment={DeploymentName}, RetryAttempt={RetryAttempt}, DelayMs={DelayMs}",
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
                    "LoanFinancialsAIAgent encountered 429-compatible exception. Deployment={DeploymentName}, RetryAttempt={RetryAttempt}, DelayMs={DelayMs}",
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
                    "LoanFinancialsAIAgent run failed. Deployment={DeploymentName}, Endpoint={Endpoint}, PromptLength={PromptLength}, ExceptionType={ExceptionType}",
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
}
