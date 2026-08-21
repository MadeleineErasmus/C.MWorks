using System.Globalization;

namespace JobCardApp.Mobile.ViewModels;

/// <summary>Inverts a bool binding — e.g. showing a "locked" hint when a CanEdit* flag is false.</summary>
public class InverseBoolConverter : IValueConverter
{
    public static readonly InverseBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}
