using System.Text.Json;

using Azure.Messaging.ServiceBus;

using FinWise.BudgetingAgent.Models;
using FinWise.BudgetingAgent.Services;
using FinWise.Shared.Core;
using FinWise.Shared.Core.A2A;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinWise.BudgetingAgent.A2A;

public class FinancialA2AFunctions(
    BudgetingAgentOrchestrator agentService,
    ServiceBusClient sbClient,
    AgentOptions options,
    IConfiguration cfg,
    ILogger<FinancialA2AFunctions> logger)
{
    private readonly BudgetingAgentOrchestrator _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
    private readonly ServiceBusClient _sbClient = sbClient ?? throw new ArgumentNullException(nameof(sbClient));
    private readonly ILogger<FinancialA2AFunctions> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AgentOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _topicName = cfg["ServiceBus:Topic"] ?? "agent-messages";

    [Function("FinancialA2A")]
    public async Task HandleFinancialRequestsAsync(
        [ServiceBusTrigger("%ServiceBus:Topic%", "%ServiceBus:Subscriptions:Financial%", Connection = "ServiceBus:Connection")]
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
                _logger.LogInformation("FinancialA2A received type={Type}, from={Sender}, to={Recipient}, corrId={CorrelationId}", type, sender, recipient, correlationId);
            }

            // Ignore response messages to prevent .Result ping-pong loops on broad subscriptions.
            if (type.EndsWith(".Result", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("FinancialA2A ignoring response message type={Type}, corrId={CorrelationId}", type, correlationId);
                await actions.CompleteMessageAsync(message);
                return;
            }

            // Ignore messages not targeted to this agent when topic subscriptions are not filtered.
            if (!string.IsNullOrWhiteSpace(recipient) && !string.Equals(recipient, _options.Id, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("FinancialA2A ignoring message for recipient={Recipient}, thisAgent={AgentId}, corrId={CorrelationId}", recipient, _options.Id, correlationId);
                await actions.CompleteMessageAsync(message);
                return;
            }

            BudgetingAgentContext context = CreateA2AContext(correlationId);
            string resultText;
            string[] functionsCalled;
            string[] toolsCalled;

            switch (type)
            {
                case A2AMessageTypes.FinancialGetBudgetAdvice:
                    context.Operation = "provide_budget_advice";
                    resultText = await _agentService.ProvideBudgetAdviceAsync(context);
                    functionsCalled = [nameof(BudgetingAgentOrchestrator.ProvideBudgetAdviceAsync)];
                    toolsCalled = ["GetAccountSummary", "GetTransactions", "GetBudgets", "GetCategories"];
                    break;

                case A2AMessageTypes.FinancialGetBudgetSummary:
                    context.Operation = "get_budget_summary";
                    resultText = await _agentService.GetBudgetSummaryAsync(context);
                    functionsCalled = [nameof(BudgetingAgentOrchestrator.GetBudgetSummaryAsync)];
                    toolsCalled = ["GetAccountSummary", "GetTransactions", "GetBudgets"];
                    break;

                case A2AMessageTypes.FinancialCreateBudget:
                    context.Operation = "create_personalized_budget";
                    resultText = await _agentService.CreatePersonalizedBudgetAsync(context);
                    functionsCalled = [nameof(BudgetingAgentOrchestrator.CreatePersonalizedBudgetAsync)];
                    toolsCalled = ["GetAccountSummary", "GetTransactions", "GetBudgets", "GetCategories"];
                    break;

                case A2AMessageTypes.FinancialAnalyzeHealth:
                    context.Operation = "analyze_financial_health";
                    context.UserMessage = TryReadSimpleTextPayload(message) ?? "Provide overall financial health analysis.";
                    resultText = await _agentService.AnalyzeFinancialHealthAsync(context);
                    functionsCalled = [nameof(BudgetingAgentOrchestrator.AnalyzeFinancialHealthAsync)];
                    toolsCalled = ["GetAccountSummary", "GetTransactions", "GetBudgets", "GetCategories"];
                    break;

                case A2AMessageTypes.FinancialAnalyzeSpending:
                    context.Operation = "analyze_spending_patterns";
                    string timeframe = ResolveAnalyzeSpendingTimeframe(message);
                    resultText = await _agentService.AnalyzeSpendingPatternsAsync(context, timeframe);
                    functionsCalled = [nameof(BudgetingAgentOrchestrator.AnalyzeSpendingPatternsAsync)];
                    toolsCalled = ["GetTransactions", "GetCategories"];
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
                A2AMessageTypes.FinancialGetBudgetAdvice => A2AMessageTypes.FinancialGetBudgetAdviceResult,
                A2AMessageTypes.FinancialGetBudgetSummary => A2AMessageTypes.FinancialGetBudgetSummaryResult,
                A2AMessageTypes.FinancialCreateBudget => A2AMessageTypes.FinancialCreateBudgetResult,
                A2AMessageTypes.FinancialAnalyzeHealth => A2AMessageTypes.FinancialAnalyzeHealthResult,
                A2AMessageTypes.FinancialAnalyzeSpending => A2AMessageTypes.FinancialAnalyzeSpendingResult,
                _ => $"{type}.Result"
            };

            var executionPayload = BuildExecutionPayload(context, resultText, functionsCalled, toolsCalled);

            // Build response
            AgentEnvelope<AgentExecutionPayload> envelope = AgentEnvelope<AgentExecutionPayload>.Create(
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

            _logger.LogInformation("FinancialA2A responded to corrId={CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FinancialA2A error for corrId={CorrelationId}", correlationId);
            await actions.AbandonMessageAsync(message);
        }
    }

    private BudgetingAgentContext CreateA2AContext(string correlationId) => new()
    {
        UserId = "a2a-user",
        ConversationId = correlationId,
        TraceId = correlationId
    };

    private AgentExecutionPayload BuildExecutionPayload(
        BudgetingAgentContext context,
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

    private string ResolveAnalyzeSpendingTimeframe(ServiceBusReceivedMessage message)
    {
        try
        {
            string body = message.Body.ToString();
            if (!string.IsNullOrWhiteSpace(body))
            {
                var env = JsonSerializer.Deserialize<AgentEnvelope<FinancialAnalyzeSpendingArgs>>(body, _jsonOptions);
                if (!string.IsNullOrWhiteSpace(env?.Payload.Timeframe))
                    return env.Payload.Timeframe;
            }
        }
        catch
        {
            // fallback to app property or default
        }

        return message.ApplicationProperties.TryGetValue("timeframe", out object? t)
            ? t?.ToString() ?? "last-month"
            : "last-month";
    }

    private string? TryReadSimpleTextPayload(ServiceBusReceivedMessage message)
    {
        try
        {
            string body = message.Body.ToString();
            if (string.IsNullOrWhiteSpace(body))
                return null;

            var env = JsonSerializer.Deserialize<AgentEnvelope<SimpleTextPayload>>(body, _jsonOptions);
            return string.IsNullOrWhiteSpace(env?.Payload.Text) ? null : env.Payload.Text;
        }
        catch
        {
            return null;
        }
    }
}
