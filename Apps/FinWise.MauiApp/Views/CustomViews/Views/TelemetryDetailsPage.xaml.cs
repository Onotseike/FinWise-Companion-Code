using FinWise.MauiApp.Views.CustomViews.ViewModels;

using Syncfusion.Maui.Toolkit.BottomSheet;

namespace FinWise.MauiApp.Views.CustomViews.Views;

public partial class TelemetryDetailsPage : SfBottomSheet
{
    public TelemetryDetailsPage() => InitializeComponent();

    #region Public Properties

    internal TelemetryDetailsViewModel? TelemetryDetails
    {
        get => GetValue(TelemetryDetailsProperty) as TelemetryDetailsViewModel;
        set => SetValue(TelemetryDetailsProperty, value);
    }

    #endregion

    #region Dependency properties

    public static readonly BindableProperty TelemetryDetailsProperty =
        BindableProperty.Create(
            nameof(TelemetryDetails),
            typeof(TelemetryDetailsViewModel),
            typeof(TelemetryDetailsPage),
            default(TelemetryDetailsViewModel),
            BindingMode.TwoWay,
            null,
            OnTelemetryDetailsChanged);

    private static void OnTelemetryDetailsChanged(BindableObject bindable, object oldValue, object newValue) { }
    #endregion

    private void Button_Clicked(object sender, EventArgs e) => this.Close();
}