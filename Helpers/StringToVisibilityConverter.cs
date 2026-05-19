using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GuildTracker.Helpers;

/// <summary>
/// Converts a string comparison to Visibility.
/// Shows the element when the bound value matches the ConverterParameter.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
