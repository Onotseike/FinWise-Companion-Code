using System.Text.Json;

using Azure.Messaging.ServiceBus;

using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FinWise.Shared.Core.AgentFramework;
using FinWise.Shared.Core;
using FinWise.Shared.Core.A2A;
using FinWise.Shared.Core.Pricing;
using FinWise.Shared.Core.Telemetry;
using FinWise.SupervisorAgent.Models;

namespace FinWise.SupervisorAgent.Services;

/// <summary>
/// Supervisor Agent Orchestrator that orchestrates multi-agent workflows.
/// Responsible for: intent routing, agent delegation, conversation orchestration.
/// Delegates AI model invocation to SupervisorFinancialsAIAgent for token tracking and metrics.
/// </summary>
public class SupervisorAgentOrchestrator(
    AIAgent? agent,
    AgentOptions options,
    SupervisorFinancialsAIAgent supervisorFinancialsAIAgent,
    ServiceBusClient sbClient,
    ILogger<SupervisorAgentOrchestrator> logger,
    IServiceProvider services) : BaseAgent<SupervisorContext>(agent, options, logger, services)
{
    private readonly SupervisorFinancialsAIAgent _supervisorFinancialsAIAgent = supervisorFinancialsAIAgent ?? throw new ArgumentNullException(nameof(supervisorFinancialsAIAgent));
    private readonly ServiceBusClient _sbClient = sbClient ?? throw new ArgumentNullException(nameof(sbClient));
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private string TopicName =>
        (Services.GetService(typeof(IConfiguration)) is IConfiguration cfg
            ? cfg["ServiceBus:Topic"]
            : null) ?? "agent-messages";

    private string SupervisorSubscription =>
        (Services.GetService(typeof(IConfiguration)) is IConfiguration cfg
            ? cfg["ServiceBus:Subscriptions:Supervisor"]
            : null) ?? "supervisor";

    /// <summary>
    /// Orchestrates a multi-agent conversation by routing the user request to appropriate agents.
    /// </summary>
    public async Task<string> OrchestrateMulitAgentRequestAsync(
        SupervisorContext context,
        CancellationToken cancellationToken = default)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Orchestrating multi-agent request. ConversationId={ConversationId}, UserId={UserId}, TraceId={TraceId}",
                context.ConversationId, context.UserId, context.TraceId);
        }

        try
        {
            // Determine intent and routing (respect pre-set direct routes from SupervisorRoute endpoint)
            var route = context.CurrentRoute;
            if (route == null || string.IsNullOrWhiteSpace(route.TargetAgent))
            {
                route = await DetermineIntentRouteAsync(context, cancellationToken);
                context.CurrentRoute = route;
            }

            if (route == null)
            {
                return "Unable to determine the intent of your request. Could you please provide more details?";
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "Route determined: Target={TargetAgent}, Confidence={Confidence}, IntentCategory={IntentCategory}",
                    route.TargetAgent, route.Confidence, route.IntentCategory);
            }

            if (!string.IsNullOrWhiteSpace(route.TargetAgent))
                context.RoutedAgents.Add(route.TargetAgent);

            foreach (string secondaryAgent in route.SecondaryAgents.Where(a => !string.IsNullOrWhiteSpace(a)))
                context.RoutedAgents.Add(secondaryAgent);

            // Delegate to target agent(s)
            var response = await DelegateToAgentAsync(route.TargetAgent, context, cancellationToken);

            // For multi-agent workflows, coordinate across agents
            if (route.RequiresMultiAgentCoordination && route.SecondaryAgents.Count > 0)
            {
                response = await CoordinateMultiAgentWorkflowAsync(route, context, response, cancellationToken);
            }

            return response.ResponseText ?? "No response from agent";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error orchestrating multi-agent request for ConversationId={ConversationId}", context.ConversationId);
            throw;
        }
    }

    /// <summary>
    /// Analyzes the user message and determines which agent should handle it.
    /// Uses LLM-based intent classification for accurate routing with confidence scores.
    /// Falls back to rule-based routing if LLM classification fails or is disabled.
    /// </summary>
    private async Task<IntentRoute?> DetermineIntentRouteAsync(
        SupervisorContext context,
        CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Determining intent route for message: {UserMessage}", context.UserMessage);

        try
        {
            // Check if LLM-based routing is enabled (default: true for production)
            bool useLlmRouting = Services.GetService(typeof(IConfiguration)) is IConfiguration config
                && config["SupervisorAgent:UseLlmIntentClassification"] != "false";

            if (useLlmRouting)
            {
                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("Using LLM-based intent classification for routing");
                var llmRoute = await DetermineIntentRouteLlmAsync(context, cancellationToken);
                if (llmRoute != null)
                    return llmRoute;

                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        "LLM-based intent classification failed. Falling back to rule-based routing. ConversationId={ConversationId}",
                        context.ConversationId);
                }
            }

            // Fallback: Rule-based routing for simple keyword matching
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("Using rule-based keyword matching for routing");
            return DetermineIntentRouteRuleBased(context);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error determining intent route");
            return null;
        }
    }

    /// <summary>
    /// LLM-BASED INTENT CLASSIFICATION (Step 1 of Migration)
    /// 
    /// Invokes the LLM to analyze user intent and determine target agent.
    /// This is the modern approach that uses AI for intelligent routing.
    /// 
    /// Token Cost: ~50-200 tokens per routing decision (varies by message length)
    /// Alternative: Could cache results for similar queries to reduce token usage
    /// </summary>
    private async Task<IntentRoute?> DetermineIntentRouteLlmAsync(
        SupervisorContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Create structured routing prompt for the LLM
            var routingPrompt = $$"""
            You are a financial request router. Analyze this user request and determine which agent should handle it.

            User Request: "{{context.UserMessage}}"

            Available Agents:
            1. budgeting - Handles budget analysis, expense tracking, financial planning, summary reporting
            2. loan - Handles mortgage analysis, property loans, financing

            IMPORTANT: Detect these specific operations:
            - "summary" or "brief overview" → intentCategory="summary"
            - "budget advice" or "recommendations" → intentCategory="advice"
            - "spending analysis" or "breakdown" → intentCategory="spending_analysis"
            - "create budget" → intentCategory="budget_creation"

            Respond in this exact JSON format:
            {
              "targetAgent": "budgeting|loan",
              "intentCategory": "summary|advice|spending_analysis|budget_creation|general",
              "confidence": 0.0-1.0,
              "reasoning": "brief explanation of routing decision",
              "requiresMultiAgent": true|false,
              "secondaryAgents": ["budgeting"] or ["loan"] or []
            }

            Make your best decision based on the user's request.
            """;

            // Step 2: Invoke SupervisorFinancialsAIAgent to get LLM response
            // This uses the infrastructure agent with token tracking!
            var response = await _supervisorFinancialsAIAgent.AnalyzeSupervisorRequestAsync(
                routingPrompt,
                route: "intent_classification",
                experimentPhase: "llm-routing-v1",
                scenario: "user-intent-analysis",
                workflowId: context.ConversationId,
                hopId: $"routing-{context.UserId}"
            );

            // Track routing tokens in context for auditing
            context.InputTokens = response.Usage?.InputTokenCount ?? 0;
            context.OutputTokens = response.Usage?.OutputTokenCount ?? 0;
            context.TokensUsed = response.Usage?.TotalTokenCount ?? 0;
            context.LastResponseId = response.ResponseId;
            context.LastInvokedAt = DateTime.UtcNow;

            // Calculate estimated tokens for fallback
            long estimatedInputTokens = EstimateTokenCount(routingPrompt);
            long estimatedOutputTokens = EstimateTokenCount(response.Text);
            long estimatedTotalTokens = estimatedInputTokens + estimatedOutputTokens;

            // Build supervisor token measurement
            context.SupervisorTokens = FinWise.Shared.Core.Telemetry.TokenMeasurement.Create(
                exactInputTokens: context.InputTokens,
                exactOutputTokens: context.OutputTokens,
                estimatedInputTokens: estimatedInputTokens,
                estimatedOutputTokens: estimatedOutputTokens,
                mode: context.TokenMeasurementMode,
                costUsd: 0m
            );
            context.ExactUsageAvailable = response.Usage is not null;

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "LLM routing decision. ResponseId={ResponseId}, InputTokens={InputTokens}, OutputTokens={OutputTokens}",
                    response.ResponseId, context.InputTokens, context.OutputTokens);
            }

            // Step 3: Parse LLM response and extract routing decision
            var routingJson = ExtractJsonFromResponse(response.Text);
            if (routingJson == null)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        "Failed to parse routing JSON from LLM response. ResponseId={ResponseId}, RawText={RawText}",
                        response.ResponseId, response.Text[..Math.Min(200, response.Text.Length)]);
                }
                return null;
            }

            // Step 4: Build IntentRoute from LLM decision
            var route = new IntentRoute
            {
                TargetAgent = routingJson["targetAgent"]?.ToString() ?? "budgeting",
                IntentCategory = routingJson["intentCategory"]?.ToString() ?? "general",
                Confidence = decimal.TryParse(routingJson["confidence"]?.ToString() ?? "0", out var conf) ? conf : 0.5m,
                RequiresMultiAgentCoordination = bool.TryParse(routingJson["requiresMultiAgent"]?.ToString() ?? "false", out var multi) && multi,
                SpecialInstructions = routingJson["reasoning"]?.ToString() ?? ""
            };

            // Add secondary agents if multi-agent coordination needed
            if (route.RequiresMultiAgentCoordination && routingJson["secondaryAgents"] is System.Collections.IEnumerable secondaryList)
            {
                route.SecondaryAgents = secondaryList.Cast<object>().Select(a => a.ToString() ?? "").ToList() ?? [];
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "LLM routing decision made. TargetAgent={TargetAgent}, Confidence={Confidence}, IntentCategory={IntentCategory}",
                    route.TargetAgent, route.Confidence, route.IntentCategory);
            }

            return route;
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    ex,
                    "Error in LLM-based intent classification. ConversationId={ConversationId}, Message={Message}",
                    context.ConversationId, ex.Message);
            }
            return null;
        }
    }

    /// <summary>
    /// RULE-BASED INTENT CLASSIFICATION (Legacy Fallback)
    /// 
    /// Keyword-based routing for when LLM classification is disabled or fails.
    /// This is lightweight and zero-cost (no LLM tokens).
    /// Routes between budgeting and loan agents based on user query keywords.
    /// 
    /// Token Cost: $0 (no AI invocation)
    /// Use When: Running in cost-constrained environments or for MVP/testing
    /// </summary>
    private static IntentRoute DetermineIntentRouteRuleBased(SupervisorContext context)
    {
        var messageLower = context.UserMessage.ToLowerInvariant();
        var route = new IntentRoute();

        // Check for loan/mortgage-related keywords first (more specific)
        if (messageLower.Contains("loan") || messageLower.Contains("mortgage") || messageLower.Contains("property")
            || messageLower.Contains("home") || messageLower.Contains("house") || messageLower.Contains("lending"))
        {
            route.TargetAgent = "loan";
            route.IntentCategory = "loan_analysis";
            route.Confidence = 0.90m;
        }
        // Default to budgeting for all other financial queries
        else if (messageLower.Contains("budget") || messageLower.Contains("expense") || messageLower.Contains("financial")
            || messageLower.Contains("money") || messageLower.Contains("spend") || messageLower.Contains("save")
            || messageLower.Contains("income") || messageLower.Contains("payment"))
        {
            route.TargetAgent = "budgeting";
            route.IntentCategory = "budget_analysis";
            route.Confidence = 0.85m;
        }
        else
        {
            // Default to budgeting for ambiguous financial queries
            route.TargetAgent = "budgeting";
            route.IntentCategory = "general_financial";
            route.Confidence = 0.60m;
        }

        return route;
    }

    /// <summary>
    /// Helper: Extract JSON object from LLM response text.
    /// Handles cases where LLM includes extra text alongside JSON.
    /// </summary>
    private static System.Collections.Generic.Dictionary<string, object?>? ExtractJsonFromResponse(string responseText)
    {
        try
        {
            // Find JSON object in response (handles markdown code blocks)
            int jsonStart = responseText.IndexOf('{');
            int jsonEnd = responseText.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = responseText.Substring(jsonStart, jsonEnd - jsonStart + 1);
                return System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object?>>(jsonStr);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string BuildUserMessageWithHistory(SupervisorContext context)
    {
        string latestUserMessage = context.MessageHistory.LastOrDefault(m => string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))?.Content
            ?? context.UserMessage
            ?? "Please provide assistance.";

        IEnumerable<ConversationMessage> priorTurns = context.MessageHistory
            .Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(6);

        if (priorTurns.Count() <= 1)
            return latestUserMessage;

        var historyBlock = string.Join("\n", priorTurns.Select(m => $"{m.Role}: {m.Content}"));
        return $"Conversation history (most recent turns):\n{historyBlock}\n\nLatest user request: {latestUserMessage}";
    }

    /// <summary>
    /// Delegates a request to a specialist agent via Service Bus (Pattern A — Supervisor Orchestration).
    ///
    /// For loan requests that benefit from financial context, the Supervisor first calls
    /// BudgetingAgent to fetch <see cref="LoanFinancialContext"/>, then passes the enriched
    /// payload to LoanAgent — agents never call each other directly.
    /// </summary>
    private async Task<AgentDelegationResponse> DelegateToAgentAsync(
        string targetAgent,
        SupervisorContext context,
        CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Delegating request to agent: {TargetAgent}", targetAgent);

        try
        {
            var agentDescriptor = AgentRegistry.GetAgent(targetAgent);
            if (agentDescriptor == null)
            {
                return new AgentDelegationResponse
                {
                    SourceAgent = targetAgent,
                    IsError = true,
                    ErrorMessage = $"Agent '{targetAgent}' not found in registry"
                };
            }

            string userMessage = BuildUserMessageWithHistory(context);

            string responseText;

            if (targetAgent.Equals("loan", StringComparison.OrdinalIgnoreCase))
            {
                // For loan requests: pre-fetch financial context from BudgetingAgent first,
                // then delegate to LoanAgent with the enriched payload.
                LoanFinancialContext? financialCtx = await FetchFinancialContextAsync(userMessage, context, cancellationToken);

                var loanArgs = new LoanAnalysisArgs(
                    UserMessage: userMessage,
                    FinancialContext: financialCtx,
                    UserId: context.UserId);

                var loanResponse = await SendAndAwaitResponseAsync(
                    context,
                    agentId: Options.Id,
                    recipient: "loan",
                    messageType: A2AMessageTypes.LoanGetMortgageOptions,
                    payload: loanArgs,
                    correlationId: context.TraceId ?? Guid.NewGuid().ToString("N"),
                    cancellationToken: cancellationToken);
                responseText = loanResponse.Text;
            }
            else
            {
                // Budget-only request: detect if it's a summary request
                bool isSummaryRequest = context.CurrentRoute?.IntentCategory?.Equals("summary", StringComparison.OrdinalIgnoreCase) ?? false;

                // Use appropriate message type and payload based on intent
                string budgetingMessageType;
                object budgetingPayload;

                if (isSummaryRequest)
                {
                    // Brief summary request - use the new summary message type
                    budgetingMessageType = A2AMessageTypes.FinancialGetBudgetSummary;
                    budgetingPayload = new SimpleTextPayload(userMessage);
                }
                else
                {
                    // Full analysis request - check for spending breakdown
                    bool useSpendingBreakdown = ShouldUseSpendingBreakdown(userMessage);
                    budgetingMessageType = useSpendingBreakdown
                        ? A2AMessageTypes.FinancialAnalyzeSpending
                        : A2AMessageTypes.FinancialGetBudgetAdvice;

                    budgetingPayload = useSpendingBreakdown
                        ? new FinancialAnalyzeSpendingArgs(Timeframe: userMessage, UserId: context.UserId)
                        : new SimpleTextPayload(userMessage);
                }

                var budgetResponse = await SendAndAwaitResponseAsync(
                    context,
                    agentId: Options.Id,
                    recipient: "budgeting",
                    messageType: budgetingMessageType,
                    payload: budgetingPayload,
                    correlationId: context.TraceId ?? Guid.NewGuid().ToString("N"),
                    cancellationToken: cancellationToken);
                responseText = budgetResponse.Text;
            }

            return new AgentDelegationResponse
            {
                SourceAgent = targetAgent,
                ResponseText = responseText,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(ex, "Error delegating to agent: {TargetAgent}", targetAgent);
            return new AgentDelegationResponse
            {
                SourceAgent = targetAgent,
                IsError = true,
                ErrorMessage = ex.Message
            };
        }
    }

    private static bool ShouldUseSpendingBreakdown(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return false;

        string text = userMessage.ToLowerInvariant();

        bool hasBreakdownIntent = text.Contains("breakdown")
            || text.Contains("analy")
            || text.Contains("spending")
            || text.Contains("income")
            || text.Contains("strictly")
            || text.Contains("range")
            || text.Contains("from") && text.Contains("to");

        bool hasDateHint = text.Contains("20")
            || text.Contains("january") || text.Contains("february") || text.Contains("march") || text.Contains("april")
            || text.Contains("may") || text.Contains("june") || text.Contains("july") || text.Contains("august")
            || text.Contains("september") || text.Contains("october") || text.Contains("november") || text.Contains("december");

        return hasBreakdownIntent && hasDateHint;
    }

    /// <summary>
    /// Fetches financial context from BudgetingAgent via Service Bus before passing it
    /// to LoanAgent. Returns <c>null</c> if the call fails — LoanAgent can still operate
    /// without financial context, just with reduced advice quality.
    /// </summary>
    private async Task<LoanFinancialContext?> FetchFinancialContextAsync(
        string userMessage,
        SupervisorContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            string correlationId = Guid.NewGuid().ToString("N");
            var financialResponse = await SendAndAwaitResponseAsync(
                context,
                agentId: Options.Id,
                recipient: "budgeting",
                messageType: A2AMessageTypes.FinancialAnalyzeHealth,
                payload: new SimpleTextPayload(userMessage),
                correlationId: correlationId,
                cancellationToken: cancellationToken);
            string rawResponse = financialResponse.Text;

            // The BudgetingAgent returns a plain text summary; wrap it in a minimal context
            // so LoanAgent can reference it in the enriched prompt.
            return new LoanFinancialContext(
                UserId: context.UserId ?? "unknown",
                MonthlyIncome: 0,
                MonthlyExpenses: 0,
                AvailableSavings: 0,
                DebtCommitments: 0,
                CreditScore: 0,
                BudgetAdvice: new Dictionary<string, object?> { { "summary", rawResponse } });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not fetch financial context from BudgetingAgent — proceeding without it");
            return null;
        }
    }

    /// <summary>
    /// Sends a typed <see cref="AgentEnvelope{TPayload}"/> to the <c>agent-messages</c> topic
    /// and awaits the correlated response via <c>AcceptSessionAsync</c>.
    /// </summary>
    private async Task<AgentExecutionResult> SendAndAwaitResponseAsync<TPayload>(
        SupervisorContext context,
        string agentId,
        string recipient,
        string messageType,
        TPayload payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var envelope = AgentEnvelope<TPayload>.Create(
            type: messageType,
            sender: agentId,
            recipient: recipient,
            correlationId: correlationId,
            payload: payload);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(envelope, _jsonOptions);
        string topic = TopicName;

        ServiceBusSender? sender = _sbClient.CreateSender(topic);
        ServiceBusMessage request = new(new BinaryData(body))
        {
            SessionId = correlationId,
            CorrelationId = correlationId,
            ContentType = "application/json"
        };
        request.ApplicationProperties["MessageType"] = messageType;
        request.ApplicationProperties["Sender"] = agentId;
        request.ApplicationProperties["Recipient"] = recipient;

        Logger.LogInformation(
            "Supervisor sending {MessageType} to {Recipient} corrId={CorrelationId}",
            messageType, recipient, correlationId);

        RecordHop(context, Options.Id, recipient, messageType, "request", correlationId);
        await sender.SendMessageAsync(request, cancellationToken);

        // Wait for the correlated response on the supervisor subscription
        await using ServiceBusSessionReceiver? sessionReceiver = await _sbClient.AcceptSessionAsync(
            topic,
            SupervisorSubscription,
            sessionId: correlationId,
            cancellationToken: cancellationToken);

        string expectedResponseType = $"{messageType}.Result";
        DateTime timeoutAt = DateTime.UtcNow.AddSeconds(30);

        while (true)
        {
            TimeSpan remaining = timeoutAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                throw new TimeoutException($"Timed out waiting for {expectedResponseType} response (corrId={correlationId})");

            ServiceBusReceivedMessage responseMsg = await sessionReceiver.ReceiveMessageAsync(remaining, cancellationToken)
                ?? throw new TimeoutException($"Timed out waiting for {expectedResponseType} response (corrId={correlationId})");

            string incomingType = responseMsg.ApplicationProperties.TryGetValue("MessageType", out object? typeObj)
                ? typeObj?.ToString() ?? string.Empty
                : string.Empty;
            string incomingSender = responseMsg.ApplicationProperties.TryGetValue("Sender", out object? senderObj)
                ? senderObj?.ToString() ?? string.Empty
                : string.Empty;
            string incomingRecipient = responseMsg.ApplicationProperties.TryGetValue("Recipient", out object? recipientObj)
                ? recipientObj?.ToString() ?? string.Empty
                : string.Empty;

            bool isExpectedResponse = string.Equals(incomingType, expectedResponseType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(incomingRecipient, Options.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(incomingSender, recipient, StringComparison.OrdinalIgnoreCase);

            if (!isExpectedResponse)
            {
                Logger.LogInformation(
                    "Supervisor skipping non-response message type={MessageType}, from={Sender}, to={Recipient}, corrId={CorrelationId}",
                    incomingType,
                    incomingSender,
                    incomingRecipient,
                    correlationId);
                await sessionReceiver.CompleteMessageAsync(responseMsg, cancellationToken);
                continue;
            }

            string responseBody = responseMsg.Body.ToString();
            await sessionReceiver.CompleteMessageAsync(responseMsg, cancellationToken);

            RecordHop(context, incomingSender, Options.Id, incomingType, "response", correlationId);

            var richResponse = JsonSerializer.Deserialize<AgentEnvelope<AgentExecutionPayload>>(responseBody, _jsonOptions);
            if (richResponse?.Payload is not null)
            {
                AppendExecutionTelemetry(context, recipient, messageType, richResponse.Payload);
                return new AgentExecutionResult(richResponse.Payload.Text, richResponse.Payload);
            }

            var simpleResponse = JsonSerializer.Deserialize<AgentEnvelope<SimpleTextPayload>>(responseBody, _jsonOptions);
            string text = simpleResponse?.Payload?.Text is { Length: > 0 } parsedText ? parsedText : responseBody;
            return new AgentExecutionResult(text, null);
        }
    }

    private void AppendExecutionTelemetry(
        SupervisorContext context,
        string recipient,
        string messageType,
        AgentExecutionPayload payload)
    {
        context.FunctionsCalled.AddRange(payload.FunctionsCalled);
        context.ToolsCalled.AddRange(payload.ToolsCalled);

        // Create token measurement from agent response
        var tokenUsage = payload.TokenUsage;

        // Use the agent's exact fields if populated, otherwise fall back to legacy fields
        long? exactInput = tokenUsage.ExactInputTokens ?? (tokenUsage.ExactUsageAvailable ? tokenUsage.InputTokens : null);
        long? exactOutput = tokenUsage.ExactOutputTokens ?? (tokenUsage.ExactUsageAvailable ? tokenUsage.OutputTokens : null);

        // Use agent's estimated fields if provided, otherwise estimate from fallback logic
        long estimatedInput = tokenUsage.EstimatedInputTokens > 0 
            ? tokenUsage.EstimatedInputTokens 
            : (long)Math.Ceiling((payload.FunctionsCalled.Length * 10 + payload.ToolsCalled.Length * 5) / 4.0);
        long estimatedOutput = tokenUsage.EstimatedOutputTokens > 0 
            ? tokenUsage.EstimatedOutputTokens 
            : (long)Math.Ceiling(payload.Text.Length / 4.0);

        var tokenMeasurement = FinWise.Shared.Core.Telemetry.TokenMeasurement.Create(
            exactInputTokens: exactInput,
            exactOutputTokens: exactOutput,
            estimatedInputTokens: estimatedInput,
            estimatedOutputTokens: estimatedOutput,
            mode: context.TokenMeasurementMode,
            costUsd: tokenUsage.InvocationCostUsd ?? 0m
        );

        context.AgentHops.Add(new AgentHopTelemetry
        {
            // Copy token measurement properties
            ExactInputTokens = tokenMeasurement.ExactInputTokens,
            ExactOutputTokens = tokenMeasurement.ExactOutputTokens,
            ExactTotalTokens = tokenMeasurement.ExactTotalTokens,
            EstimatedInputTokens = tokenMeasurement.EstimatedInputTokens,
            EstimatedOutputTokens = tokenMeasurement.EstimatedOutputTokens,
            EstimatedTotalTokens = tokenMeasurement.EstimatedTotalTokens,
            Mode = tokenMeasurement.Mode,
            ExactUsageAvailable = tokenMeasurement.ExactUsageAvailable,
            CostUsd = tokenMeasurement.CostUsd,
            // Add hop-specific metadata
            AgentId = recipient,
            MessageType = messageType,
            Operation = payload.Operation,
            FunctionsCalled = payload.FunctionsCalled,
            ToolsCalled = payload.ToolsCalled,
            ResponseId = tokenUsage.ResponseId,
            ModelName = tokenUsage.ModelName,
            PricingTier = tokenUsage.PricingTier
        });

        // Aggregate downstream tokens into the TokenMeasurement object
        if (context.DownstreamTokens == null)
        {
            context.DownstreamTokens = tokenMeasurement;
        }
        else
        {
            // Aggregate with existing downstream tokens
            context.DownstreamTokens = TokenMeasurement.Aggregate(context.DownstreamTokens, tokenMeasurement);
        }
        context.DownstreamTokens.Mode = context.TokenMeasurementMode;
    }

    private static void RecordHop(
        SupervisorContext context,
        string fromAgent,
        string toAgent,
        string messageType,
        string direction,
        string correlationId)
    {
        context.HopSequenceCounter++;
        context.HopSequence.Add(new AgentHopSequenceItem
        {
            Step = context.HopSequenceCounter,
            FromAgent = fromAgent,
            ToAgent = toAgent,
            MessageType = messageType,
            Direction = direction,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow
        });
    }

    private sealed record AgentExecutionResult(string Text, AgentExecutionPayload? Payload);

    /// <summary>
    /// Coordinates workflow across multiple agents when multi-agent coordination is needed.
    /// </summary>
    private async Task<AgentDelegationResponse> CoordinateMultiAgentWorkflowAsync(
        IntentRoute route,
        SupervisorContext context,
        AgentDelegationResponse primaryResponse,
        CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Coordinating multi-agent workflow. Primary={PrimaryAgent}, Secondary={SecondaryCount}",
                route.TargetAgent, route.SecondaryAgents.Count);
        }

        var secondaryResponses = new List<AgentDelegationResponse>();

        foreach (var secondaryAgent in route.SecondaryAgents)
        {
            var response = await DelegateToAgentAsync(secondaryAgent, context, cancellationToken);
            secondaryResponses.Add(response);
        }

        // Aggregate responses from primary and secondary agents
        var combinedResponse = new AgentDelegationResponse
        {
            SourceAgent = route.TargetAgent,
            ResponseText = AggregateAgentResponses(primaryResponse, secondaryResponses),
            ResponseMetadata = new Dictionary<string, object?>
            {
                { "primaryAgent", route.TargetAgent },
                { "secondaryAgentsCount", secondaryResponses.Count },
                { "coordinationTimestamp", DateTime.UtcNow }
            }
        };

        return combinedResponse;
    }

    /// <summary>
    /// Aggregates responses from multiple agents into a cohesive response.
    /// </summary>
    private static string AggregateAgentResponses(
        AgentDelegationResponse primary,
        List<AgentDelegationResponse> secondary)
    {
        if (secondary.Count == 0)
            return primary.ResponseText ?? string.Empty;

        var aggregated = new System.Text.StringBuilder();
        _ = aggregated.AppendLine("**Primary Analysis:**");
        _ = aggregated.AppendLine(primary.ResponseText);

        _ = aggregated.AppendLine("\n**Additional Insights:**");
        foreach (var response in secondary.Where(r => !r.IsError))
        {
            _ = aggregated.AppendLine($"- {response.ResponseText}");
        }

        return aggregated.ToString();
    }

    /// <summary>
    /// Implements abstract method from BaseAgent to provide agent-specific invoke logic.
    /// Delegates AI model invocation to SupervisorFinancialsAIAgent which handles:
    /// - Token tracking (exact vs estimated vs hybrid)
    /// - Retry logic with exponential backoff
    /// - Metrics emission to Application Insights
    /// </summary>
    protected override async Task<string> InvokeAgentAsync(
        string userMessage,
        SupervisorContext context,
        CancellationToken cancellationToken = default)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Invoking SupervisorFinancialsAIAgent via SupervisorAgentOrchestrator. ConversationId={ConversationId}, UserId={UserId}, Operation={Operation}",
                context.ConversationId, context.UserId, context.Operation);
        }

        try
        {
            // Delegate to SupervisorFinancialsAIAgent for AI model invocation with token tracking
            var response = await _supervisorFinancialsAIAgent.AnalyzeSupervisorRequestAsync(
                userMessage,
                route: context.Operation ?? "orchestrator",
                experimentPhase: "baseline",
                scenario: context.Operation ?? "general",
                workflowId: context.ConversationId,
                hopId: $"supervisor-{context.UserId}"
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

                if (Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(
                        "Cost calculation completed. Cost={Cost:F6} USD, InputCost={InputCost:F6}, OutputCost={OutputCost:F6}, CumulativeSessionCost={SessionCost:F6} USD, PricingTier={Tier}",
                        costResult.TotalCostUsd, costResult.InputCostUsd, costResult.OutputCostUsd, context.CumulativeSessionCostUsd, costResult.PricingTier);
                }
            }
            catch (Exception costEx)
            {
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    Logger.LogWarning(
                        costEx,
                        "Failed to calculate cost. Will continue without cost tracking. ConversationId={ConversationId}",
                        context.ConversationId);
                }
            }

            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "AI invocation completed. ResponseId={ResponseId}, MessageCount={MessageCount}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, TotalTokens={TotalTokens}, InvocationCost={InvocationCost:F6} USD",
                    response.ResponseId, response.Messages.Count, inputTokens, outputTokens, totalTokens, context.LastInvocationCostUsd ?? 0);
            }

            // Orchestrate multi-agent request for routing
            return await OrchestrateMulitAgentRequestAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    ex,
                    "Error invoking SupervisorFinancialsAIAgent. ConversationId={ConversationId}, Operation={Operation}",
                    context.ConversationId, context.Operation);
            }
            throw;
        }
    }

    /// <summary>
    /// Implements abstract method to persist supervisor context to Cosmos DB.
    /// </summary>
    protected override Task PersistStateAsync(
        SupervisorContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement Cosmos DB persistence for conversation context
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Persisting conversation context. ConversationId={ConversationId}", context.ConversationId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Implements abstract method to hydrate context from Cosmos DB.
    /// </summary>
    protected override Task HydrateContextAsync(
        SupervisorContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement Cosmos DB retrieval for existing conversation context
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Hydrating conversation context. ConversationId={ConversationId}", context.ConversationId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Estimates token count using the same formula as BudgetingAgent and LoanAgent (text.Length / 4.0).
    /// This is a simplified approximation - actual token counts may vary.
    /// </summary>
    private static int EstimateTokenCount(string? text) => 
        string.IsNullOrWhiteSpace(text) ? 0 : (int)Math.Ceiling(text.Length / 4.0);
}
