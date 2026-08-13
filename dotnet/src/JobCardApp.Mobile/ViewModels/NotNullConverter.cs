using System.Globalization;

namespace JobCardApp.Mobile.ViewModels;

/// <summary>Shows an element only when the bound value is not null/empty.</summary>
public class NotNullConverter : IValueConverter
{
    public static readonly NotNullConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s ? !string.IsNullOrWhiteSpace(s) : value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
