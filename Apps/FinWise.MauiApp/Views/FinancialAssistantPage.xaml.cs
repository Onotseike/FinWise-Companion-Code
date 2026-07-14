using FinWise.MauiApp.Views.CustomViews.ViewModels;

using Syncfusion.Maui.Chat;

namespace FinWise.MauiApp.Views;

public partial class FinancialAssistantPage : ContentPage
{
    private readonly FinancialAssistantViewModel _viewModel;

    public FinancialAssistantPage(FinancialAssistantViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    private async void OnMoreOptionsClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheetAsync(
            "Options",
            "Cancel",
            null,
            "View Telemetry",
            "Clear Conversation",
            "Check Supervisor Health",
            "Load Sample Questions");

        switch (action)
        {
            case "View Telemetry":
                await _viewModel.ShowTelemetryForMessageCommand.ExecuteAsync(null);
                break;
            case "Clear Conversation":
                _viewModel.ClearConversationCommand.Execute(null);
                break;
            case "Check Supervisor Health":
                await _viewModel.CheckSupervisorHealthCommand.ExecuteAsync(null);
                break;
            case "Load Sample Questions":
                _viewModel.InitializeConversationCommand.Execute(null);
                break;
        }
    }

    private void OnTelemetryInfoToggle(object sender, EventArgs e)
    {
        // Toggle the visibility of the telemetry info content
        if (telemetryInfoContent.IsVisible)
        {
            telemetryInfoContent.IsVisible = false;
            telemetryInfoToggle.Text = "▶";
        }
        else
        {
            telemetryInfoContent.IsVisible = true;
            telemetryInfoToggle.Text = "▼";
        }
    }

    private void ChatControl_SuggestionItemSelected(object sender, Syncfusion.Maui.Chat.SuggestionItemSelectedEventArgs e) => e.HideAfterSelection = false;

    private async void ChatControl_MessageDoubleTapped(object sender, Syncfusion.Maui.Chat.MessageDoubleTappedEventArgs e)
    {
        if (e.Message is TextMessage selectedMsg)
        {
            if (selectedMsg.Data is not MessageWithTelemetry telemetryMessage)
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

            _viewModel.SelectedTelemetry = new TelemetryDetailsViewModel();
            _viewModel.SelectedTelemetry.LoadTelemetry(telemetryMessage.Telemetry, telemetryMessage.MessageText, telemetryMessage.SelectedAgent ?? string.Empty, telemetryMessage.Timestamp);
            telemetryDetailsSheet.BindingContext = _viewModel.SelectedTelemetry;
            telemetryDetailsSheet.Show();
        }

    }
}
