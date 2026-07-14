using FinWise.MauiApp.Views.CustomViews.ViewModels;
using FinWise.MauiApp.Views.CustomViews.Views;

using Microsoft.Extensions.Configuration;

using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace FinWise.MauiApp;

public static class MauiProgram
{
    public static MauiAppType CreateMauiApp()
    {
        var builder = MauiAppType.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .ConfigureSyncfusionToolkit()// Add Syncfusion configuration
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("FontAwesome6FreeBrands.otf", "FontAwesomeBrands");
                fonts.AddFont("FontAwesome6FreeRegular.otf", "FontAwesomeRegular");
                fonts.AddFont("FontAwesome6FreeSolid.otf", "FontAwesomeSolid");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add configuration
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
#if ANDROID
            ["SupervisorAgent:BaseUrl"] = "https://<supervisor-agent-host>", // SupervisorAgent base URL
#elif IOS

            // Configure SupervisorAgent endpoints
            ["SupervisorAgent:BaseUrl"] = "https://<supervisor-agent-host>",
#else
            ["SupervisorAgent:BaseUrl"] = "https://<supervisor-agent-host>", // SupervisorAgent base URL
#endif
            ["SupervisorAgent:FunctionKey"] = "<supervisor-agent-function-key>",
        });

        // Register SupervisorAgentHttpClient
        builder.Services.AddHttpClient<SupervisorAgentHttpClient>();
        builder.Services.AddSingleton<SupervisorAgentHttpClient>();

        // Register ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<FinancialAssistantViewModel>();
        builder.Services.AddTransient<DirectAgentRoutingViewModel>();
        builder.Services.AddTransient<TelemetryDetailsViewModel>();

        // Register Pages
        builder.Services.AddTransient<FinancialAssistantPage>();
        builder.Services.AddTransient<DirectAgentRoutingPage>();
        builder.Services.AddTransient<TelemetryDetailsPage>();

        // Register existing services
        builder.Services.AddTransient<SampleDataService>();

        return builder.Build();
    }
}
