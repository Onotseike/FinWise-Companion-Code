using Foundation;

namespace FinWise.MauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiAppType CreateMauiApp() => MauiProgram.CreateMauiApp();
}
