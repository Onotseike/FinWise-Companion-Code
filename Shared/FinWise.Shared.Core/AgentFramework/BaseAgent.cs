using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FinWise.Shared.Core.AgentFramework;

/// <summary>
/// Abstract base class for all FinWise agents. Provides standardized patterns for:
/// - Tool invocation and error handling
/// - State management and persistence
/// - Logging and diagnostics
/// - Token usage tracking
/// </summary>
/// <typeparam name="TContext">The agent-specific context type for conversation state</typeparam>
/// <remarks>
/// Initializes a new instance of the BaseAgent class.
/// </remarks>
/// <param name="agent">The underlying AIAgent instance (nullable for orchestration agents like Supervisor)</param>
/// <param name="options">Agent configuration options</param>
/// <param name="logger">Logger instance for diagnostics</param>
/// <param name="services">Service provider for dependency resolution</param>
public abstract class BaseAgent<TContext>(
    AIAgent? agent,
    AgentOptions options,
    ILogger<BaseAgent<TContext>> logger,
    IServiceProvider services) where TContext : class
{
    protected readonly AIAgent? Agent = agent;
    protected readonly AgentOptions Options = options ?? throw new ArgumentNullException(nameof(options));
    protected readonly ILogger<BaseAgent<TContext>> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IServiceProvider Services = services ?? throw new ArgumentNullException(nameof(services));

    /// <summary>
    /// Gets the agent's unique identifier.
    /// </summary>
    public string AgentId => Options.Id;

    /// <summary>
    /// Gets the underlying AIAgent instance. May be null for orchestration agents (like Supervisor).
    /// </summary>
    protected AIAgent? UnderlyingAgent => Agent;

    /// <summary>
    /// Invokes the agent with the provided user message and context, returning a response.
    /// Handles tool calling, error management, and diagnostic logging.
    /// </summary>
    /// <param name="userMessage">The user's input message</param>
    /// <param name="context">The agent-specific context (conversation state, user info, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent's response text</returns>
    /// <exception cref="AgentInvocationException">Thrown when the agent invocation fails after retries</exception>
    public async Task<string> InvokeWithContextAsync(
        string userMessage,
        TContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("User message cannot be empty", nameof(userMessage));
        }

        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "Invoking agent {AgentId} with message (length: {MessageLength}). Context type: {ContextType}",
                    AgentId, userMessage.Length, typeof(TContext).Name);
            }

            // Call the protected virtual method for agent-specific invocation logic
            var response = await InvokeAgentAsync(userMessage, context, cancellationToken) ?? throw new AgentInvocationException(
                    $"Agent {AgentId} returned null response", null, AgentId);
            stopwatch.Stop();
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "Agent {AgentId} completed invocation in {ElapsedMs}ms. Response length: {ResponseLength}",
                    AgentId, stopwatch.ElapsedMilliseconds, response.Length);
            }

            return response;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    ex,
                    "Agent {AgentId} invocation was cancelled after {ElapsedMs}ms",
                    AgentId, stopwatch.ElapsedMilliseconds);
            }
            throw new AgentInvocationException(
                $"Agent {AgentId} invocation was cancelled", ex, AgentId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode?.ToString() == "429")
        {
            stopwatch.Stop();
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    ex,
                    "Agent {AgentId} rate limited (429). Elapsed: {ElapsedMs}ms",
                    AgentId, stopwatch.ElapsedMilliseconds);
            }
            throw new AgentInvocationException(
                $"Agent {AgentId} rate limited. Retry after: {ex.Message}", ex, AgentId, isRetryable: true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    ex,
                    "Agent {AgentId} invocation failed after {ElapsedMs}ms. Message: {Message}",
                    AgentId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
            throw new AgentInvocationException(
                $"Agent {AgentId} invocation failed: {ex.Message}", ex, AgentId);
        }
    }

    /// <summary>
    /// Protected virtual method that subclasses override to implement agent-specific invocation logic.
    /// </summary>
    /// <param name="userMessage">The user's input message</param>
    /// <param name="context">The agent-specific context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The agent's response text</returns>
    protected abstract Task<string> InvokeAgentAsync(
        string userMessage,
        TContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called after a successful invocation to allow subclasses to persist state.
    /// </summary>
    /// <param name="context">The updated context after invocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected virtual Task PersistStateAsync(TContext context, CancellationToken cancellationToken = default) =>
        // Default: no-op. Subclasses can override to implement state persistence (e.g., to Cosmos DB)
        Task.CompletedTask;

    /// <summary>
    /// Called before invocation to allow subclasses to load or hydrate state.
    /// </summary>
    /// <param name="context">The context to be hydrated</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    protected virtual Task HydrateContextAsync(TContext context, CancellationToken cancellationToken = default) =>
        // Default: no-op. Subclasses can override to load state from persistence (e.g., from Cosmos DB)
        Task.CompletedTask;

    /// <summary>
    /// Logs diagnostic information about agent invocation (tool calls, latency, token usage).
    /// </summary>
    /// <param name="toolName">The name of the tool invoked</param>
    /// <param name="inputTokens">Number of input tokens (if available from LLM response)</param>
    /// <param name="outputTokens">Number of output tokens (if available from LLM response)</param>
    /// <param name="latencyMs">Invocation latency in milliseconds</param>
    protected void LogDiagnostics(
        string toolName,
        int? inputTokens = null,
        int? outputTokens = null,
        long latencyMs = 0)
    {
        if (!Logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var diagnostics = new DiagnosticEntry
        {
            AgentId = AgentId,
            ToolName = toolName,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            LatencyMilliseconds = latencyMs,
            Timestamp = DateTime.UtcNow
        };

        Logger.LogInformation(
            "Diagnostics: Agent={AgentId}, Tool={ToolName}, Tokens=({InputTokens}/{OutputTokens}), Latency={LatencyMs}ms",
            diagnostics.AgentId,
            diagnostics.ToolName,
            diagnostics.InputTokens ?? -1,
            diagnostics.OutputTokens ?? -1,
            diagnostics.LatencyMilliseconds);
    }
}

/// <summary>
/// Exception thrown when an agent invocation fails.
/// </summary>
/// <remarks>
/// Initializes a new instance of the AgentInvocationException class.
/// </remarks>
public class AgentInvocationException(
    string message,
    Exception? innerException = null,
    string agentId = "unknown",
    bool isRetryable = false) : Exception(message, innerException)
{
    /// <summary>
    /// Gets the ID of the agent that failed.
    /// </summary>
    public string AgentId { get; } = agentId;

    /// <summary>
    /// Gets a value indicating whether the error is retryable.
    /// </summary>
    public bool IsRetryable { get; } = isRetryable;
}

/// <summary>
/// Represents diagnostic information logged for agent invocations.
/// </summary>
public record DiagnosticEntry
{
    public string AgentId { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public long LatencyMilliseconds { get; set; }
    public DateTime Timestamp { get; set; }
}
