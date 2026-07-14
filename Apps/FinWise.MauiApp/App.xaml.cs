namespace FinWise.MauiApp;

public partial class App : Application
{
    public App()
    {
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXlfdHRcRWNdUU12X0VWYEo=");

        InitializeComponent();

        // Uncomment the following as a quick way to test loading resources for different languages
        // CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es");
    }

    protected override Window CreateWindow(IActivationState? activationState) => new Window(new AppShell());
}
