using System.Text;

using FinWise.Shared.Core.Telemetry;

namespace FinWise.MauiApp.Views.CustomViews.ViewModels;

public partial class TelemetryDetailsViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial string MessageContent { get; set; }

    [ObservableProperty]
    public partial string SelectedAgent { get; set; }

    [ObservableProperty]
    public partial string ConversationId { get; set; }

    [ObservableProperty]
    public partial DateTime MessageTimestamp { get; set; }

    [ObservableProperty]
    public partial string AgentHopsDetail { get; set; }

    [ObservableProperty]
    public partial string FunctionsCalledDetail { get; set; }

    [ObservableProperty]
    public partial string ToolsCalledDetail { get; set; }

    [ObservableProperty]
    public partial string RoutingDetail { get; set; }

    [ObservableProperty]
    public partial string TokenUsageDetail { get; set; }

    [ObservableProperty]
    public partial string TopicsDetail { get; set; }

    [ObservableProperty]
    public partial string ConversationHistoryDetail { get; set; }

    [ObservableProperty]
    public partial string TraceIdDetail { get; set; }

    /// <summary>
    /// Formatted token table data for Supervisor (Exact | Estimated | Measured).
    /// </summary>
    [ObservableProperty]
    public partial TokenMeasurement SupervisorTokenTable { get; set; }

    /// <summary>
    /// Formatted token table data for Downstream (Exact | Estimated | Measured).
    /// </summary>
    [ObservableProperty]
    public partial TokenMeasurement DownstreamTokenTable { get; set; }

    /// <summary>
    /// Formatted token table data for Total (Exact | Estimated | Measured).
    /// </summary>
    [ObservableProperty]
    public partial TokenMeasurement TotalTokenTable { get; set; }

    /// <summary>
    /// Measurement mode string for display(Exact, Estimated, Hybrid)..
    /// </summary>
    [ObservableProperty]
    public partial string MeasurementModeDisplay { get; set; }

    /// <summary>
    /// Warning message if exact usage not available.
    /// </summary>
    [ObservableProperty]
    public partial string TokenWarningMessage { get; set; }

    public TelemetryDetailsViewModel()
    {
        Title = "Message Telemetry Details";
        MessageContent = string.Empty;
        SelectedAgent = string.Empty;
        ConversationId = string.Empty;
        AgentHopsDetail = string.Empty;
        FunctionsCalledDetail = string.Empty;
        ToolsCalledDetail = string.Empty;
        RoutingDetail = string.Empty;
        TokenUsageDetail = string.Empty;
        TopicsDetail = string.Empty;
        ConversationHistoryDetail = string.Empty;
        TraceIdDetail = string.Empty;
        SupervisorTokenTable = new TokenMeasurement();
        DownstreamTokenTable = new TokenMeasurement();
        TotalTokenTable = new TokenMeasurement();
        MeasurementModeDisplay = string.Empty;
        TokenWarningMessage = string.Empty;
    }

    /// <summary>
    /// Loads telemetry data from an EnhancedSupervisorResponse.
    /// </summary>
    public void LoadTelemetry(EnhancedSupervisorResponse response, string messageContent, string selectedAgent, DateTime timestamp)
    {
        MessageContent = messageContent;
        SelectedAgent = selectedAgent;
        ConversationId = response.ConversationId;
        MessageTimestamp = timestamp;

        // Format agent hops
        FormatAgentHops(response);

        // Format functions called
        FormatFunctionsCalled(response);

        // Format tools called
        FormatToolsCalled(response);

        // Format routing details
        FormatRoutingDetails(response);

        // Format token usage
        FormatTokenUsage(response);

        // Format topics
        FormatTopics(response);

        // Format conversation history
        FormatConversationHistory(response);

        // Set trace ID
        TraceIdDetail = response.TraceId ?? "N/A";
    }

    private void FormatAgentHops(EnhancedSupervisorResponse response)
    {
        if (response.AgentHops is not { Count: > 0 })
        {
            AgentHopsDetail = "No agent hops recorded.";
            return;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine($"Total Hops: {response.AgentHops.Count}");
        _ = sb.AppendLine();

        for (int i = 0; i < response.AgentHops.Count; i++)
        {
            var hop = response.AgentHops[i];
            _ = sb.AppendLine($"HOP {i + 1}: {hop.From} → {hop.To}");
            _ = sb.AppendLine($"  Type: {hop.MessageType}");
            _ = sb.AppendLine($"  Direction: {hop.Direction}");

            if (!string.IsNullOrEmpty(hop.Operation))
                _ = sb.AppendLine($"  Operation: {hop.Operation}");

            _ = sb.AppendLine($"  Timestamp: {hop.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");

            if (!string.IsNullOrEmpty(hop.CorrelationId))
                _ = sb.AppendLine($"  Correlation ID: {hop.CorrelationId}");

            // Show per-hop token telemetry if available
            if (hop.TokenUsage is { MeasuredTotalTokens: > 0 })
            {
                _ = sb.AppendLine($"  📊 Tokens (Measured):");
                _ = sb.AppendLine($"    Input: {hop.TokenUsage.MeasuredInputTokens:N0}  Output: {hop.TokenUsage.MeasuredOutputTokens:N0}  Total: {hop.TokenUsage.MeasuredTotalTokens:N0}");

                // Show measurement source
                if (hop.TokenUsage.ExactUsageAvailable)
                {
                    _ = sb.AppendLine($"    (Exact from provider)");
                }
            }

            if (hop.FunctionsCalled?.Count > 0)
                _ = sb.AppendLine($"  Functions: {string.Join(", ", hop.FunctionsCalled)}");

            if (hop.ToolsCalled?.Count > 0)
                _ = sb.AppendLine($"  Tools: {string.Join(", ", hop.ToolsCalled)}");

            _ = sb.AppendLine();
        }

        AgentHopsDetail = sb.ToString().TrimEnd();
    }

    private void FormatFunctionsCalled(EnhancedSupervisorResponse response)
    {
        if (response.FunctionsCalled == null || response.FunctionsCalled.Count == 0)
        {
            FunctionsCalledDetail = "No functions were called.";
            return;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine($"Total Functions: {response.FunctionsCalled.Count}");
        _ = sb.AppendLine();

        for (int i = 0; i < response.FunctionsCalled.Count; i++)
        {
            _ = sb.AppendLine($"{i + 1}. {response.FunctionsCalled[i]}");
        }

        FunctionsCalledDetail = sb.ToString().TrimEnd();
    }

    private void FormatToolsCalled(EnhancedSupervisorResponse response)
    {
        if (response.ToolsCalled == null || response.ToolsCalled.Count == 0)
        {
            ToolsCalledDetail = "No external tools were invoked.";
            return;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine($"Total Tools: {response.ToolsCalled.Count}");
        _ = sb.AppendLine();

        for (int i = 0; i < response.ToolsCalled.Count; i++)
        {
            _ = sb.AppendLine($"{i + 1}. {response.ToolsCalled[i]}");
        }

        ToolsCalledDetail = sb.ToString().TrimEnd();
    }

    private void FormatRoutingDetails(EnhancedSupervisorResponse response)
    {
        if (response.Routing == null)
        {
            RoutingDetail = "No routing details available.";
            return;
        }

        var sb = new StringBuilder();
        var routing = response.Routing;

        if (!string.IsNullOrEmpty(routing.PrimaryAgent))
            _ = sb.AppendLine($"Primary Agent: {routing.PrimaryAgent}");

        if (!string.IsNullOrEmpty(routing.IntentCategory))
            _ = sb.AppendLine($"Intent Category: {routing.IntentCategory}");

        if (routing.ExtractedParameters is Dictionary<string, object?> parameters && parameters.Count > 0)
        {
            _ = sb.AppendLine($"Extracted Parameters:");
            foreach (var param in parameters)
            {
                _ = sb.AppendLine($"  {param.Key}: {param.Value}");
            }
        }

        if (!string.IsNullOrEmpty(routing.SpecialInstructions))
            _ = sb.AppendLine($"Special Instructions: {routing.SpecialInstructions}");

        RoutingDetail = sb.Length == 0 ? "No routing details available." : sb.ToString().TrimEnd();
    }

    private void FormatTokenUsage(EnhancedSupervisorResponse response)
    {
        if (response.TokenUsage == null)
        {
            TokenUsageDetail = "No token usage data available.";
            SupervisorTokenTable = new TokenMeasurement();
            DownstreamTokenTable = new TokenMeasurement();
            TotalTokenTable = new TokenMeasurement();
            MeasurementModeDisplay = string.Empty;
            TokenWarningMessage = string.Empty;
            return;
        }

        var tokenUsage = response.TokenUsage;

        // Set measurement mode display
        MeasurementModeDisplay = $"Measurement Mode: {tokenUsage.Mode}";

        // Set warning message for fallback scenarios
        TokenWarningMessage = tokenUsage.Mode switch
        {
            TokenMeasurementMode.Hybrid when !tokenUsage.ExactUsageAvailable
                => "⚠️ Note: No exact tokens from provider - showing estimated values",
            TokenMeasurementMode.Exact when !tokenUsage.ExactUsageAvailable
                => "⚠️ Note: Exact mode selected but exact tokens are unavailable for one or more hops",
            _ => string.Empty
        };

        // Format token tables for each tier
        SupervisorTokenTable = tokenUsage.Supervisor is not null ? tokenUsage.Supervisor : new TokenMeasurement();

        DownstreamTokenTable = tokenUsage.Downstream is not null ? tokenUsage.Downstream : new TokenMeasurement();

        TotalTokenTable = tokenUsage.Total is not null ? tokenUsage.Total : new TokenMeasurement();

        // Also keep the original text format for backward compatibility
        var sb = new StringBuilder();
        _ = sb.AppendLine($"Measurement Mode: {tokenUsage.Mode}");
        if (!tokenUsage.ExactUsageAvailable && tokenUsage.Mode == TokenMeasurementMode.Hybrid)
        {
            _ = sb.AppendLine("⚠️ Note: No exact tokens from provider - showing estimated values");
        }
        _ = sb.AppendLine();

        // Supervisor metrics
        if (tokenUsage.Supervisor is not null)
        {
            _ = sb.AppendLine("SUPERVISOR:");
            AppendTokenTierDetails(sb, tokenUsage.Supervisor, tokenUsage.Mode, "  ");

            if (tokenUsage.Supervisor.CostUsd > 0)
            {
                _ = sb.AppendLine($"  Cost: ${tokenUsage.Supervisor.CostUsd:F6}");
            }
            _ = sb.AppendLine();
        }

        // Downstream metrics
        if (tokenUsage.Downstream is not null)
        {
            _ = sb.AppendLine("DOWNSTREAM AGENTS:");
            AppendTokenTierDetails(sb, tokenUsage.Downstream, tokenUsage.Mode, "  ");

            if (tokenUsage.Downstream.CostUsd > 0)
            {
                _ = sb.AppendLine($"  Cost: ${tokenUsage.Downstream.CostUsd:F6}");
            }
            _ = sb.AppendLine();
        }

        // Total metrics
        if (tokenUsage.Total is not null)
        {
            _ = sb.AppendLine("TOTAL (Supervisor + Downstream):");
            AppendTokenTierDetails(sb, tokenUsage.Total, tokenUsage.Mode, "  ");

            if (tokenUsage.Total.CostUsd > 0)
            {
                _ = sb.AppendLine($"  Total Cost: ${tokenUsage.Total.CostUsd:F6}");
            }
        }

        TokenUsageDetail = sb.ToString().TrimEnd();
    }

    private static void AppendTokenTierDetails(StringBuilder sb, TokenMeasurement tier, TokenMeasurementMode mode, string indent)
    {
        switch (mode)
        {
            case TokenMeasurementMode.Exact:
                _ = sb.AppendLine($"{indent}Exact Input:  {tier.ExactInputTokens ?? 0:N0} tokens");
                _ = sb.AppendLine($"{indent}Exact Output: {tier.ExactOutputTokens ?? 0:N0} tokens");
                _ = sb.AppendLine($"{indent}Exact Total:  {tier.ExactTotalTokens ?? 0:N0} tokens");
                break;
            case TokenMeasurementMode.Estimated:
                _ = sb.AppendLine($"{indent}Estimated Input:  {tier.EstimatedInputTokens:N0} tokens");
                _ = sb.AppendLine($"{indent}Estimated Output: {tier.EstimatedOutputTokens:N0} tokens");
                _ = sb.AppendLine($"{indent}Estimated Total:  {tier.EstimatedTotalTokens:N0} tokens");
                break;
            default:
                _ = sb.AppendLine($"{indent}Measured Input:  {tier.MeasuredInputTokens:N0} tokens ({tier.MeasuredSource})");
                _ = sb.AppendLine($"{indent}Measured Output: {tier.MeasuredOutputTokens:N0} tokens ({tier.MeasuredSource})");
                _ = sb.AppendLine($"{indent}Measured Total:  {tier.MeasuredTotalTokens:N0} tokens");
                _ = sb.AppendLine($"{indent}Exact Tally:      {tier.ExactInputTokens ?? 0:N0} in + {tier.ExactOutputTokens ?? 0:N0} out = {tier.ExactTotalTokens ?? 0:N0} total");
                _ = sb.AppendLine($"{indent}Estimated Tally:  {tier.EstimatedInputTokens:N0} in + {tier.EstimatedOutputTokens:N0} out = {tier.EstimatedTotalTokens:N0} total");
                break;
        }
    }

    /// <summary>
    /// Formats a token tier (Supervisor/Downstream/Total) as a readable table with columns for Exact | Estimated | Measured.
    /// </summary>
    private string FormatTokenTierTable(string tierName, FinWise.Shared.Core.Telemetry.TokenMeasurement tier, FinWise.Shared.Core.Telemetry.TokenMeasurementMode mode)
    {
        var sb = new StringBuilder();

        // Tier header
        _ = sb.AppendLine(tierName);
        _ = sb.AppendLine("─────────────────────────────────────────────────────────────────────");

        // Column headers
        _ = sb.AppendLine("┌─────────┬──────────────────┬──────────────────┬──────────────────┐");
        _ = sb.AppendLine("│ Type    │       EXACT      │     ESTIMATED    │     MEASURED     │");
        _ = sb.AppendLine("├─────────┼──────────────────┼──────────────────┼──────────────────┤");

        // Input row
        var exactIn = tier.ExactInputTokens.HasValue ? tier.ExactInputTokens.Value.ToString("N0") : "—";
        var estIn = tier.EstimatedInputTokens.ToString("N0");
        var measIn = tier.MeasuredInputTokens.ToString("N0");
        _ = sb.AppendLine($"│ Input   │ {exactIn,16} │ {estIn,16} │ {measIn,16} │");

        // Output row
        var exactOut = tier.ExactOutputTokens.HasValue ? tier.ExactOutputTokens.Value.ToString("N0") : "—";
        var estOut = tier.EstimatedOutputTokens.ToString("N0");
        var measOut = tier.MeasuredOutputTokens.ToString("N0");
        _ = sb.AppendLine($"│ Output  │ {exactOut,16} │ {estOut,16} │ {measOut,16} │");

        // Total row
        var exactTotal = tier.ExactTotalTokens.HasValue ? tier.ExactTotalTokens.Value.ToString("N0") : "—";
        var estTotal = tier.EstimatedTotalTokens.ToString("N0");
        var measTotal = tier.MeasuredTotalTokens.ToString("N0");
        _ = sb.AppendLine($"│ Total   │ {exactTotal,16} │ {estTotal,16} │ {measTotal,16} │");

        _ = sb.AppendLine("└─────────┴──────────────────┴──────────────────┴──────────────────┘");

        // Measurement source indicator
        _ = sb.AppendLine($"📊 Measurement Source: {tier.MeasuredSource}");

        // Cost if available
        if (tier.CostUsd > 0)
        {
            _ = sb.AppendLine($"💰 Cost: ${tier.CostUsd:F6}");
        }

        return sb.ToString().TrimEnd();
    }

    private void FormatTopics(EnhancedSupervisorResponse response)
    {
        if (response.Topics == null || response.Topics.Count == 0)
        {
            TopicsDetail = "No topics identified.";
            return;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine($"Topics Identified ({response.Topics.Count}):");
        _ = sb.AppendLine();

        for (int i = 0; i < response.Topics.Count; i++)
        {
            _ = sb.AppendLine($"{i + 1}. {response.Topics[i]}");
        }

        TopicsDetail = sb.ToString().TrimEnd();
    }

    private void FormatConversationHistory(EnhancedSupervisorResponse response)
    {
        if (response.Turns == null || response.Turns.Count == 0)
        {
            ConversationHistoryDetail = "No conversation history available.";
            return;
        }

        var sb = new StringBuilder();
        _ = sb.AppendLine($"Conversation History ({response.Turns.Count} turns):");
        _ = sb.AppendLine();

        for (int i = 0; i < response.Turns.Count; i++)
        {
            var turn = response.Turns[i];
            _ = sb.AppendLine($"Turn {i + 1} ({turn.Role}):");
            _ = sb.AppendLine($"  Timestamp: {turn.Timestamp:yyyy-MM-dd HH:mm:ss}");

            // Truncate long content for display
            string content = turn.Content ?? string.Empty;
            if (content.Length > 200)
            {
                content = content.Substring(0, 200) + "...";
            }
            _ = sb.AppendLine($"  Content: {content}");
            _ = sb.AppendLine();
        }

        ConversationHistoryDetail = sb.ToString().TrimEnd();
    }

}
