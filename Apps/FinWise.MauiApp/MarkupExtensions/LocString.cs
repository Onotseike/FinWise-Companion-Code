using FinWise.MauiApp.Resources.Strings;

namespace FinWise.MauiApp.MarkupExtensions;

[ContentProperty(nameof(Name))]
public class LocString : IMarkupExtension<string>
{
	public string? Name { get; set; }

    string IMarkupExtension<string>.ProvideValue(IServiceProvider serviceProvider) => string.IsNullOrEmpty(Name) ? string.Empty : AppResources.ResourceManager?.GetString(Name) ?? string.Empty;

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => string.IsNullOrEmpty(Name) ? string.Empty : AppResources.ResourceManager?.GetString(Name) ?? string.Empty;
}
