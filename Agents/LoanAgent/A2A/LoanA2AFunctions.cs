using System.Text.Json;

using Azure.Messaging.ServiceBus;

using FinWise.LoanAgent.Models;
using FinWise.LoanAgent.Services;
using FinWise.Shared.Core;
using FinWise.Shared.Core.A2A;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinWise.LoanAgent.A2A;

/// <summary>
/// Azure Functions Service Bus trigger that receives A2A messages from the SupervisorAgent
/// on the <c>agent-messages</c> topic / <c>loan</c> subscription.
///
/// Routing (Pattern A — Supervisor Orchestration):
///   SupervisorAgent → Service Bus (agent-messages / loan subscription) → LoanA2AFunctions → LoanAgentOrchestrator
///
/// The Supervisor may pre-fetch financial context from BudgetingAgent and embed it inside
/// the <see cref="LoanAnalysisArgs"/> payload so this agent can provide enriched advice
/// without any direct agent-to-agent calls.
/// </summary>
public class LoanA2AFunctions(
    LoanAgentOrchestrator agentService,
    ServiceBusClient sbClient,
    AgentOptions options,
    IConfiguration cfg,
    ILogger<LoanA2AFunctions> logger)
{
    private readonly LoanAgentOrchestrator _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
    private readonly ServiceBusClient _sbClient = sbClient ?? throw new ArgumentNullException(nameof(sbClient));
    private readonly ILogger<LoanA2AFunctions> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AgentOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _topicName = cfg["ServiceBus:Topic"] ?? "agent-messages";

    [Function("LoanA2A")]
    public async Task HandleLoanRequestsAsync(
        [ServiceBusTrigger("%ServiceBus:Topic%", "%ServiceBus:Subscriptions:Loan%", Connection = "ServiceBus:Connection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions actions)
    {
        string type = message.ApplicationProperties.TryGetValue("MessageType", out object? t) ? t?.ToString() ?? "" : "";
        string sender = message.ApplicationProperties.TryGetValue("Sender", out object? s) ? s?.ToString() ?? "" : "";
        string recipient = message.ApplicationProperties.TryGetValue("Recipient", out object? r) ? r?.ToString() ?? "" : "";
        string correlationId = message.CorrelationId ?? Guid.NewGuid().ToString();

        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "LoanA2A received type={Type}, from={Sender}, to={Recipient}, corrId={CorrelationId}",
                    type, sender, recipient, correlationId);
            }

            // Ignore response messages to prevent .Result ping-pong loops on broad subscriptions.
            if (type.EndsWith(".Result", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("LoanA2A ignoring response message type={Type}, corrId={CorrelationId}", type, correlationId);
                await actions.CompleteMessageAsync(message);
                return;
            }

            // Ignore messages not targeted to this agent when topic subscriptions are not filtered.
            if (!string.IsNullOrWhiteSpace(recipient) && !string.Equals(recipient, _options.Id, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("LoanA2A ignoring message for recipient={Recipient}, thisAgent={AgentId}, corrId={CorrelationId}", recipient, _options.Id, correlationId);
                await actions.CompleteMessageAsync(message);
                return;
            }

            // Deserialize the envelope once so all handlers can use the structured args
            LoanAnalysisArgs? args = TryDeserializeArgs(message.Body.ToString());

            FinancialContext? financialContext = MapFinancialContext(args?.FinancialContext);
            string userMessage = args?.UserMessage ?? "Provide mortgage advice.";
            string? userId = args?.UserId;

            var context = CreateA2AContext(correlationId, userMessage, userId, financialContext);
            string[] functionsCalled;
            string[] toolsCalled;
            string resultText;

            switch (type)
            {
                case A2AMessageTypes.LoanGetMortgageOptions:
                    context.Operation = "comprehensive_mortgage_advice";
                    resultText = await _agentService.ProvideComprehensiveMortgageAdviceAsync(context);
                    functionsCalled = [nameof(LoanAgentOrchestrator.ProvideComprehensiveMortgageAdviceAsync)];
                    toolsCalled = ["GetMortgagePaymentEstimate", "GetAffordabilityRecommendation"];
                    break;

                case A2AMessageTypes.LoanCompareMortgages:
                    context.Operation = "compare_mortgages";
                    resultText = await _agentService.AnalyzeMortgageOptionsAsync(context);
                    functionsCalled = [nameof(LoanAgentOrchestrator.CompareMortgagesAsync)];
                    toolsCalled = ["GetMortgageComparison", "GetAffordabilityRecommendation"];
                    break;

                case A2AMessageTypes.LoanGetPropertyAdvice:
                    context.Operation = "property_advice";
                    resultText = await _agentService.AnalyzeMortgageOptionsAsync(context);
                    functionsCalled = [nameof(LoanAgentOrchestrator.GetPropertyAdviceAsync)];
                    toolsCalled = ["GetPropertyAdvice", "GetAffordabilityRecommendation"];
                    break;

                default:
                    context.Operation = "unsupported";
                    resultText = $"Unsupported message type '{type}'";
                    functionsCalled = [];
                    toolsCalled = [];
                    break;
            }

            string responseType = type switch
            {
                A2AMessageTypes.LoanGetMortgageOptions => A2AMessageTypes.LoanGetMortgageOptionsResult,
                A2AMessageTypes.LoanCompareMortgages => A2AMessageTypes.LoanCompareMortgagesResult,
                A2AMessageTypes.LoanGetPropertyAdvice => A2AMessageTypes.LoanGetPropertyAdviceResult,
                _ => $"{type}.Result"
            };

            var executionPayload = BuildExecutionPayload(context, resultText, functionsCalled, toolsCalled);

            // Build and send response envelope back to the supervisor subscription
            var envelope = AgentEnvelope<AgentExecutionPayload>.Create(
                responseType,
                _options.Id,
                sender,
                correlationId,
                executionPayload);

            byte[] body = JsonSerializer.SerializeToUtf8Bytes(envelope, _jsonOptions);

            ServiceBusSender? senderClient = _sbClient.CreateSender(_topicName);
            ServiceBusMessage response = new(new BinaryData(body))
            {
                SessionId = correlationId,
                CorrelationId = correlationId,
                ContentType = "application/json"
            };
            response.ApplicationProperties["MessageType"] = responseType;
            response.ApplicationProperties["Sender"] = _options.Id;
            response.ApplicationProperties["Recipient"] = sender;

            await senderClient.SendMessageAsync(response);
            await actions.CompleteMessageAsync(message);

            _logger.LogInformation("LoanA2A responded to corrId={CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoanA2A error for corrId={CorrelationId}", correlationId);
            await actions.AbandonMessageAsync(message);
        }
    }

    private LoanAnalysisArgs? TryDeserializeArgs(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            var env = JsonSerializer.Deserialize<AgentEnvelope<LoanAnalysisArgs>>(body, _jsonOptions);
            return env?.Payload;
        }
        catch
        {
            return null;
        }
    }

    private LoanAgentContext CreateA2AContext(
        string correlationId,
        string userMessage,
        string? userId,
        FinancialContext? financialContext) => new()
    {
        UserId = userId ?? "a2a-user",
        ConversationId = correlationId,
        TraceId = correlationId,
        UserMessage = userMessage,
        FinancialContext = financialContext
    };

    private AgentExecutionPayload BuildExecutionPayload(
        LoanAgentContext context,
        string resultText,
        string[] functionsCalled,
        string[] toolsCalled)
    {
        long inputTokens = context.InputTokens ?? 0;
        long outputTokens = context.OutputTokens ?? 0;
        long totalTokens = context.TokensUsed ?? (inputTokens + outputTokens);

        // Calculate estimated tokens from result text length (fallback for hybrid mode)
        long estimatedOutputTokens = (long)Math.Ceiling(resultText.Length / 4.0);
        long estimatedInputTokens = (long)Math.Ceiling((functionsCalled.Length * 10 + toolsCalled.Length * 5) / 4.0); // Rough estimate

        // Only set exact values if they're non-zero (meaning the AI provider actually provided them)
        long? exactInputTokens = (context.InputTokens.HasValue && context.InputTokens.Value > 0) ? context.InputTokens : null;
        long? exactOutputTokens = (context.OutputTokens.HasValue && context.OutputTokens.Value > 0) ? context.OutputTokens : null;

        var tokenUsage = new AgentTokenUsage(
            InputTokens: inputTokens,
            OutputTokens: outputTokens,
            TotalTokens: totalTokens,
            DurationMs: 0,
            ExactUsageAvailable: exactInputTokens.HasValue || exactOutputTokens.HasValue,
            ResponseId: context.LastResponseId,
            ModelName: context.LastModelName,
            InvocationCostUsd: context.LastInvocationCostUsd,
            PricingTier: context.LastPricingTier,
            // Populate dual metrics for hybrid mode support
            ExactInputTokens: exactInputTokens,
            ExactOutputTokens: exactOutputTokens,
            EstimatedInputTokens: estimatedInputTokens,
            EstimatedOutputTokens: estimatedOutputTokens);

        return new AgentExecutionPayload(
            Text: resultText,
            Operation: context.Operation,
            FunctionsCalled: PrefixWithAgent(functionsCalled),
            ToolsCalled: toolsCalled,
            RoutedAgents: [_options.Id],
            TokenUsage: tokenUsage);
    }

    private string[] PrefixWithAgent(string[] entries)
    {
        if (entries.Length == 0)
            return [];

        string[] prefixed = new string[entries.Length];
        for (int i = 0; i < entries.Length; i++)
        {
            prefixed[i] = $"{_options.Id}.{entries[i]}";
        }

        return prefixed;
    }

    /// <summary>
    /// Maps the shared-contract <see cref="LoanFinancialContext"/> DTO to the
    /// LoanAgent domain model <see cref="FinancialContext"/>.
    /// </summary>
    private static FinancialContext? MapFinancialContext(LoanFinancialContext? src) =>
        src is null
            ? null
            : new FinancialContext
            {
                UserId = src.UserId,
                MonthlyIncome = src.MonthlyIncome,
                MonthlyExpenses = src.MonthlyExpenses,
                AvailableSavings = src.AvailableSavings,
                DebtCommitments = src.DebtCommitments,
                CreditScore = src.CreditScore,
                BudgetAdvice = src.BudgetAdvice ?? [],
                SpendingAnalysis = src.SpendingAnalysis ?? []
            };
}
