using System.Globalization;

namespace FinWise.MauiApp.Converters;

/// <summary>
/// Converter for boolean to color conversion
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool boolValue && parameter is string param
            ? param switch
            {
                "User" => boolValue ? Colors.LightBlue : Colors.LightGray,
                "Error" => boolValue ? Colors.Red : Colors.Black,
                "Selected" => boolValue ? Color.FromArgb("#4CAF50") : Color.FromArgb("#9E9E9E"),
                _ => Colors.Transparent
            }
            : Colors.Transparent;

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// Converter for boolean to FontAttributes conversion
/// </summary>
public class BoolToFontAttributeConverter : IValueConverter
{
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => value is bool boolValue && boolValue ? FontAttributes.Bold : FontAttributes.None;

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// Converter for boolean to margin conversion
/// </summary>
public class BoolToMarginConverter : IValueConverter
{
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool boolValue ? boolValue ? new Thickness(50, 5, 5, 5) : new Thickness(5, 5, 50, 5) : new Thickness(5);

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

/// <summary>
/// Converter for inverting boolean values
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool boolValue && !boolValue;

    object IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool boolValue && !boolValue;
}