using System.Text;

using FinWise.MauiApp.Views.CustomViews.ViewModels;
using FinWise.Shared.Core.Telemetry;

using Syncfusion.Maui.Chat;
using Syncfusion.Maui.DataSource.Extensions;

namespace FinWise.MauiApp.ViewModels;

/// <summary>
/// ViewModel for the Financial Assistant page with Supervisor Agent integration
/// Manages chat interactions, token tracking, and agent routing display
/// </summary>
public partial class FinancialAssistantViewModel : BaseViewModel
{
    private readonly SupervisorAgentHttpClient _supervisorClient;
    private string _currentConversationId = Guid.NewGuid().ToString();
    private const string TokenModePreferenceKey = "token_measurement_mode";

    // Syncfusion Chat requires CurrentUser
    public Author CurrentUser { get; } = new() { Name = "You", Avatar = "user.png" };
    public Author AssistantUser { get; } = new() { Name = "FinWise AI", Avatar = "assistant.png" };

    [ObservableProperty]
    public partial string ChatMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentResponse { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial TokenUsageInfo? LastTokenUsage { get; set; }

    [ObservableProperty]
    public partial string RoutedAgent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal RoutingConfidence { get; set; }

    [ObservableProperty]
    public partial string TokenSummary { get; set; }

    [ObservableProperty]
    public partial string SelectedTokenMode { get; set; } = "hybrid";

    [ObservableProperty]
    public partial int SessionTotalTokens { get; set; }

    [ObservableProperty]
    public partial decimal SessionTotalCostUsd { get; set; }

    [ObservableProperty]
    public partial EnhancedSupervisorResponse? LastEnhancedResponse { get; set; }

    [ObservableProperty]
    public partial string VisibleAgentHops { get; set; }

    [ObservableProperty]
    public partial string VisibleFunctionsCalled { get; set; }

    [ObservableProperty]
    public partial string VisibleToolsCalled { get; set; }

    [ObservableProperty]
    public partial TelemetryDetailsViewModel? SelectedTelemetry { get; set; }

    [ObservableProperty]
    public partial bool IsTelemetryDetailsVisible { get; set; }

    [ObservableProperty]
    public partial TextMessage? LastAssistantTextMessage { get; set; }

    public ObservableCollection<object> ChatMessages { get; } = [];
    public IReadOnlyList<string> TokenModes { get; } = ["hybrid", "exact", "estimated"];

    public ChatSuggestions ChatSuggestions { get; }

    public FinancialAssistantViewModel(SupervisorAgentHttpClient supervisorClient)
    {
        _supervisorClient = supervisorClient;
        Title = "Financial Assistant (Supervisor)";
        SelectedTokenMode = NormalizeTokenMode(Preferences.Get(TokenModePreferenceKey, "hybrid"));
        TokenSummary = "No tokens used yet";
        SessionTotalTokens = 0;
        SessionTotalCostUsd = 0m;
        VisibleAgentHops = string.Empty;
        VisibleFunctionsCalled = string.Empty;
        VisibleToolsCalled = string.Empty;
        LastEnhancedResponse = null;

        ChatSuggestions = new()
        {
            Items = [.. ChatHelperMethods.SampleQuestions.Select(question => new Suggestion() { Text = question })]
        };

        InitializeConversation();
    }

    [RelayCommand]
    private async Task SendChatMessageAsync(SendMessageEventArgs args)
    {

        if (args.Message is null || string.IsNullOrWhiteSpace(args.Message.Text) || IsProcessing)
            return;
        ChatMessage = args.Message.Text;
        string userMessage = ChatMessage;
        ChatMessage = string.Empty;
        args.Handled = true;
        // Add user message to Syncfusion Chat
        var userTextMessage = new TextMessage
        {
            Author = CurrentUser,
            Text = userMessage,
            DateTime = DateTime.Now,
            Data = new MessageWithTelemetry
            {
                MessageText = userMessage,
                Timestamp = DateTime.Now,
                IsUserMessage = true,
                ConversationId = _currentConversationId
            }
        };

        // Add the message to the ChatMessages collection and track telemetry
        ChatMessages.Add(userTextMessage);

        IsProcessing = true;

        try
        {
            // Create request with metadata
            var request = new SupervisorChatRequest
            {
                ConversationId = _currentConversationId,
                UserId = Preferences.Get("user_id", "anonymous_user"),
                Message = userMessage,
                TokenMeasurementMode = SelectedTokenMode,
                Metadata = new Dictionary<string, object?>
                {
                    ["source"] = "maui_app",
                    ["platform"] = DeviceInfo.Platform.ToString(),
                    ["app_version"] = AppInfo.VersionString,
                    ["token_measurement_mode"] = SelectedTokenMode
                }
            };

            // Send to supervisor and get response with full telemetry
            var supervisorResponse = await _supervisorClient.ChatAsync(request) ?? throw new InvalidOperationException("Supervisor returned no response.");

            // Store the full response for telemetry display
            LastEnhancedResponse = supervisorResponse;

            // Update routing info
            if (supervisorResponse.Routing != null)
            {
                RoutedAgent = supervisorResponse.Routing.PrimaryAgent ?? string.Empty;
                RoutingConfidence = supervisorResponse.Routing.Confidence;
            }

            // Update telemetry summaries
            UpdateTelemetrySummaries(supervisorResponse);

            var responseText = supervisorResponse.Response ?? string.Empty;

            // Add assistant response to Syncfusion Chat
            var assistantTextMessage = new TextMessage
            {
                Author = AssistantUser,
                Text = responseText,
                DateTime = DateTime.Now,
                Data = new MessageWithTelemetry
                {
                    MessageText = responseText,
                    Timestamp = DateTime.Now,
                    IsUserMessage = false,
                    Telemetry = supervisorResponse,
                    ConversationId = _currentConversationId,
                    AgentHopsSummary = VisibleAgentHops,
                    FunctionsCalledSummary = VisibleFunctionsCalled,
                    ToolsCalledSummary = VisibleToolsCalled
                }
            };
            // Add to chat
            ChatMessages.Add(assistantTextMessage);
            LastAssistantTextMessage = assistantTextMessage;
            CurrentResponse = responseText;
        }
        catch (Exception ex)
        {
            string errorMessage = $"Error: {ex.Message}";

            // Add error to Syncfusion Chat
            var errorTextMessage = new TextMessage
            {
                Author = AssistantUser,
                Text = errorMessage,
                DateTime = DateTime.Now,
                Data = new MessageWithTelemetry
                {
                    MessageText = errorMessage,
                    Timestamp = DateTime.Now,
                    IsUserMessage = false,
                    ConversationId = _currentConversationId
                }
            };

            // Create wrapper and add to chat
            ChatMessages.Add(errorTextMessage);
            LastAssistantTextMessage = errorTextMessage;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ShowTelemetryForMessageAsync(MessageBase message)
    {
        if (message.Data is not MessageWithTelemetry telemetryMessage)
        {
            await Shell.Current.DisplayAlertAsync("No Telemetry", "No telemetry data available for this message.", "OK");
            return;
        }

        // Only assistant messages have telemetry
        if (telemetryMessage.IsUserMessage)
        {
            await Shell.Current.DisplayAlertAsync(
                "No Telemetry",
                "User messages do not have telemetry data.",
                "OK");
            return;
        }

        if (telemetryMessage.Telemetry == null)
        {
            await Shell.Current.DisplayAlertAsync(
                "No Telemetry",
                "Telemetry data is not available for this message.",
                "OK");
            return;
        }

        // Set the selected telemetry for the popup to access
        SelectedTelemetry ??= new();
        SelectedTelemetry.LoadTelemetry(telemetryMessage.Telemetry, telemetryMessage.MessageText, telemetryMessage.SelectedAgent ?? string.Empty, telemetryMessage.Timestamp);
        IsTelemetryDetailsVisible = true;
    }

    [RelayCommand]
    private async Task GetMessageTelemetryOnDoubleTappedAsync(MessageDoubleTappedEventArgs args)
    {
        if (args.Message is not MessageBase telemetryMessage)
        {
            await Shell.Current.DisplayAlertAsync("No Message", "No message data available.", "OK");
            return;
        }
        await ShowTelemetryForMessageAsync(telemetryMessage);
    }

    [RelayCommand]
    private void ClearConversation()
    {
        ChatMessages.Clear();
        SelectedTelemetry = null;
        LastAssistantTextMessage = null;
        _currentConversationId = Guid.NewGuid().ToString();
        CurrentResponse = string.Empty;
        TokenSummary = "No tokens used yet";
        SessionTotalTokens = 0;
        SessionTotalCostUsd = 0m;
        RoutedAgent = string.Empty;
        RoutingConfidence = 0;
        LastTokenUsage = null;
        LastEnhancedResponse = null;
        VisibleAgentHops = string.Empty;
        VisibleFunctionsCalled = string.Empty;
        VisibleToolsCalled = string.Empty;
    }

    [RelayCommand]
    private async Task CheckSupervisorHealthAsync()
    {
        IsProcessing = true;
        try
        {
            var health = await _supervisorClient.GetHealthAsync() ?? throw new InvalidOperationException("Supervisor health endpoint returned no response.");
            string healthText = $"Supervisor Status: {health.Status}\nTimestamp: {health.Timestamp:g}";

            // Add to Syncfusion Chat
            var healthMessage = new TextMessage
            {
                Author = AssistantUser,
                Text = healthText,
                DateTime = DateTime.Now,
                Data = new MessageWithTelemetry
                {
                    MessageText = healthText,
                    Timestamp = DateTime.Now,
                    IsUserMessage = false,
                    ConversationId = _currentConversationId
                }
            };
            ChatMessages.Add(healthMessage);
            LastAssistantTextMessage = healthMessage;
        }
        catch (Exception ex)
        {
            string errorText = $"Health check failed: {ex.Message}";

            // Add error to Syncfusion Chat
            var errorMessage = new TextMessage
            {
                Author = AssistantUser,
                Text = errorText,
                DateTime = DateTime.Now,
                Data = new MessageWithTelemetry
                {
                    MessageText = errorText,
                    Timestamp = DateTime.Now,
                    IsUserMessage = false,
                    ConversationId = _currentConversationId
                }
            };
            ChatMessages.Add(errorMessage);
            LastAssistantTextMessage = errorMessage;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void InitializeConversation() => AddAssistantMessage("Hello! I'm FinWise AI. I can help with budgeting, spending insights, savings goals, and mortgage planning. Here are some sample questions");

    private void AddAssistantMessage(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var message = new TextMessage
        {
            Author = AssistantUser,
            Text = text,
            DateTime = DateTime.Now,
            //Suggestions = ChatSuggestions,
            Data = new MessageWithTelemetry
            {
                MessageText = text,
                Timestamp = DateTime.Now,
                IsUserMessage = false,
                ConversationId = _currentConversationId
            }
        };
        ChatMessages.Add(message);
        LastAssistantTextMessage = message;
    }

    private void UpdateTelemetrySummaries(EnhancedSupervisorResponse response)
    {
        if (response.TokenUsage == null)
        {
            UpdateTokenSummaryFromTokenUsage(null);
            return;
        }

        // Update token usage summary
        UpdateTokenSummaryFromTokenUsage(response.TokenUsage);

        // Update agent hops display
        if (response.AgentHops?.Count > 0)
        {
            var hopSummary = new StringBuilder();
            _ = hopSummary.AppendLine("Agent Hops:");
            for (int i = 0; i < response.AgentHops.Count; i++)
            {
                var hop = response.AgentHops[i];
                _ = hopSummary.AppendLine($"  {i + 1}. {hop.From} → {hop.To} ({hop.Direction})");
            }
            VisibleAgentHops = hopSummary.ToString().TrimEnd();
        }

        // Update functions called
        if (response.FunctionsCalled?.Count > 0)
        {
            VisibleFunctionsCalled = $"Functions: {string.Join(", ", response.FunctionsCalled.Take(3))}";
            if (response.FunctionsCalled.Count > 3)
                VisibleFunctionsCalled += $" (+{response.FunctionsCalled.Count - 3} more)";
        }

        // Update tools called
        if (response.ToolsCalled?.Count > 0)
        {
            VisibleToolsCalled = $"Tools: {string.Join(", ", response.ToolsCalled.Take(3))}";
            if (response.ToolsCalled.Count > 3)
                VisibleToolsCalled += $" (+{response.ToolsCalled.Count - 3} more)";
        }
    }

    private void UpdateTokenSummaryFromTokenUsage(EnhancedTokenUsageTelemetry? telemetry)
    {
        if (telemetry == null)
        {
            UpdateTokenSummary(null);
            return;
        }

        // Update session totals from telemetry
        if (telemetry.Total is not null)
        {
            SessionTotalTokens = (int)(SessionTotalTokens + telemetry.Total.MeasuredTotalTokens);
            SessionTotalCostUsd += telemetry.Total.CostUsd;
        }

        // Build summary text
        var summary = new StringBuilder();
        _ = summary.AppendLine("Last Request:");
        _ = summary.AppendLine($"  Mode: {telemetry.Mode}");

        if (telemetry.Supervisor is not null)
        {
            _ = summary.AppendLine($"  Supervisor:");
            AppendTokenTierSummary(summary, telemetry.Supervisor, telemetry.Mode, "    ");
        }

        if (telemetry.Downstream is not null)
        {
            _ = summary.AppendLine($"  Downstream:");
            AppendTokenTierSummary(summary, telemetry.Downstream, telemetry.Mode, "    ");
            if (telemetry.Downstream.CostUsd > 0)
                _ = summary.AppendLine($"    Cost: ${telemetry.Downstream.CostUsd:F6}");
        }

        if (telemetry.Total is not null)
        {
            _ = summary.AppendLine($"  Total:");
            AppendTokenTierSummary(summary, telemetry.Total, telemetry.Mode, "    ");
            if (telemetry.Total.CostUsd > 0)
                _ = summary.AppendLine($"    Cost: ${telemetry.Total.CostUsd:F6}");
        }

        _ = summary.AppendLine();
        _ = summary.AppendLine($"Session Totals:");
        _ = summary.AppendLine($"  Tokens: {SessionTotalTokens}");

        if (SessionTotalCostUsd > 0)
        {
            _ = summary.AppendLine($"  Cost: ${SessionTotalCostUsd:F6}");
        }

        if (!string.IsNullOrEmpty(RoutedAgent))
        {
            _ = summary.AppendLine($"  Agent: {RoutedAgent} ({RoutingConfidence:P0})");
        }

        TokenSummary = summary.ToString();
    }

    partial void OnSelectedTokenModeChanged(string value)
    {
        Preferences.Set(TokenModePreferenceKey, NormalizeTokenMode(value));
    }

    private static string NormalizeTokenMode(string? mode) =>
        mode?.Trim().ToLowerInvariant() switch
        {
            "exact" => "exact",
            "estimate" => "estimated",
            "estimated" => "estimated",
            _ => "hybrid"
        };

    private static void AppendTokenTierSummary(StringBuilder summary, TokenMeasurement measurement, TokenMeasurementMode mode, string indent)
    {
        switch (mode)
        {
            case TokenMeasurementMode.Exact:
                _ = summary.AppendLine($"{indent}Exact Input: {measurement.ExactInputTokens ?? 0:N0} tokens");
                _ = summary.AppendLine($"{indent}Exact Output: {measurement.ExactOutputTokens ?? 0:N0} tokens");
                _ = summary.AppendLine($"{indent}Exact Total: {measurement.ExactTotalTokens ?? 0:N0} tokens");
                break;
            case TokenMeasurementMode.Estimated:
                _ = summary.AppendLine($"{indent}Estimated Input: {measurement.EstimatedInputTokens:N0} tokens");
                _ = summary.AppendLine($"{indent}Estimated Output: {measurement.EstimatedOutputTokens:N0} tokens");
                _ = summary.AppendLine($"{indent}Estimated Total: {measurement.EstimatedTotalTokens:N0} tokens");
                break;
            default:
                _ = summary.AppendLine($"{indent}Measured Input: {measurement.MeasuredInputTokens:N0} tokens ({measurement.MeasuredSource})");
                _ = summary.AppendLine($"{indent}Measured Output: {measurement.MeasuredOutputTokens:N0} tokens ({measurement.MeasuredSource})");
                _ = summary.AppendLine($"{indent}Measured Total: {measurement.MeasuredTotalTokens:N0} tokens");
                _ = summary.AppendLine($"{indent}Exact Tally: {measurement.ExactInputTokens ?? 0:N0} in + {measurement.ExactOutputTokens ?? 0:N0} out = {measurement.ExactTotalTokens ?? 0:N0} total");
                _ = summary.AppendLine($"{indent}Estimated Tally: {measurement.EstimatedInputTokens:N0} in + {measurement.EstimatedOutputTokens:N0} out = {measurement.EstimatedTotalTokens:N0} total");
                break;
        }
    }

    private void UpdateTokenSummary(TokenUsageInfo? usage)
    {
        if (usage == null) return;

        SessionTotalTokens += usage.MeasuredTotalTokens;

        if (usage.EstimatedCostUsd.HasValue)
        {
            SessionTotalCostUsd += usage.EstimatedCostUsd.Value;
        }

        var summary = new StringBuilder();
        _ = summary.AppendLine($"Last Request:");
        _ = summary.AppendLine($"  Input: {usage.MeasuredInputTokens} tokens");
        _ = summary.AppendLine($"  Output: {usage.MeasuredOutputTokens} tokens");
        _ = summary.AppendLine($"  Total: {usage.MeasuredTotalTokens} tokens");

        if (usage.ExactUsageAvailable)
        {
            _ = summary.AppendLine($"  Mode: {usage.TokenMeasurementMode} (Exact)");
            if (usage.ExactInputTokens.HasValue)
            {
                _ = summary.AppendLine($"  Provider Input: {usage.ExactInputTokens}");
                _ = summary.AppendLine($"  Provider Output: {usage.ExactOutputTokens}");
            }
        }
        else
        {
            _ = summary.AppendLine($"  Mode: {usage.TokenMeasurementMode} (Estimated)");
            _ = summary.AppendLine($"  Est. Input: {usage.EstimatedInputTokens}");
            _ = summary.AppendLine($"  Est. Output: {usage.EstimatedOutputTokens}");
        }

        if (usage.EstimatedCostUsd.HasValue)
        {
            _ = summary.AppendLine($"  Cost: ${usage.EstimatedCostUsd:F4}");
        }

        _ = summary.AppendLine();
        _ = summary.AppendLine($"Session Totals:");
        _ = summary.AppendLine($"  Tokens: {SessionTotalTokens}");

        if (SessionTotalCostUsd > 0)
        {
            _ = summary.AppendLine($"  Cost: ${SessionTotalCostUsd:F4}");
        }

        if (usage.DurationMs > 0)
        {
            _ = summary.AppendLine($"  Duration: {usage.DurationMs}ms");
        }

        if (!string.IsNullOrEmpty(RoutedAgent))
        {
            _ = summary.AppendLine($"  Agent: {RoutedAgent} ({RoutingConfidence:P0})");
        }

        TokenSummary = summary.ToString();
    }

}
