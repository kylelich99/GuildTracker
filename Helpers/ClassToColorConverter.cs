using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GuildTracker.Helpers;

/// <summary>
/// Converts a class name string to a consistent color.
/// Each class always gets the same color based on its name hash.
/// </summary>
public class ClassToColorConverter : IValueConverter
{
    // Catppuccin-inspired palette - distinct, readable on dark backgrounds
    private static readonly string[] Colors =
    {
        "#f38ba8", // Red (Rosewater)
        "#fab387", // Peach
        "#f9e2af", // Yellow
        "#a6e3a1", // Green
        "#94e2d5", // Teal
        "#89dceb", // Sky
        "#89b4fa", // Blue
        "#b4befe", // Lavender
        "#cba6f7", // Mauve
        "#f5c2e7", // Pink
        "#74c7ec", // Sapphire
        "#eba0ac", // Maroon
        "#f2cdcd", // Flamingo
        "#a6e3a1", // Green
        "#f5e0dc", // Rosewater light
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string className || string.IsNullOrEmpty(className))
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cdd6f4"));

        // Use absolute value of hash to pick a consistent color
        var index = Math.Abs(className.GetHashCode()) % Colors.Length;
        var color = (Color)ColorConverter.ConvertFromString(Colors[index]);
        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
