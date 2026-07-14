namespace FinWise.MauiApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(FinancialAssistantPage), typeof(FinancialAssistantPage));
        Routing.RegisterRoute(nameof(DirectAgentRoutingPage), typeof(DirectAgentRoutingPage));
        //Routing.RegisterRoute(nameof(TelemetryDetailsPage), typeof(TelemetryDetailsPage));
    }
}
