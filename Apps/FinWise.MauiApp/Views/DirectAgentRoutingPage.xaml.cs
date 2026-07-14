using FinWise.MauiApp.Views.CustomViews.ViewModels;

using Syncfusion.Maui.Chat;

namespace FinWise.MauiApp.Views;

public partial class DirectAgentRoutingPage : ContentPage
{
    private readonly DirectAgentRoutingViewModel _viewModel;

    public DirectAgentRoutingPage(DirectAgentRoutingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args) => base.OnNavigatedTo(args);// Note: The Syncfusion Chat control doesn't expose easy message-level event handling// for individual message taps. The telemetry display is handled via the// ShowTelemetryForMessageAsync command on the ViewModel which users can invoke// programmatically or we can add a long-press handler if needed.

    private void ChatControl_SuggestionItemSelected(object sender, Syncfusion.Maui.Chat.SuggestionItemSelectedEventArgs e) => e.HideAfterSelection = false;

    private async void DirectRoutingChat_MessageDoubleTapped(object sender, Syncfusion.Maui.Chat.MessageDoubleTappedEventArgs e)
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
}
