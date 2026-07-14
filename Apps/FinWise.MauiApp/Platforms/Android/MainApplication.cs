using Android.App;
using Android.Runtime;

namespace FinWise.MauiApp;

[Application]
public class MainApplication(IntPtr handle, JniHandleOwnership ownership) : MauiApplication(handle, ownership)
{
    protected override MauiAppType CreateMauiApp() => MauiProgram.CreateMauiApp();
}
