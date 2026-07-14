namespace FinWise.MauiApp.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e) =>
        // Navigate to the FinancialAssistantPage
        await Shell.Current.GoToAsync("//llmrouting");

    private async void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e) =>
        // Navigate to the DirectAgentRoutingPage
        await Shell.Current.GoToAsync("//directrouting");
}
