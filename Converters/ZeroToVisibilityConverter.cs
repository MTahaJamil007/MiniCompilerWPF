using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MiniCompilerWPF.Converters;

public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = string.Equals(parameter?.ToString(), "Invert", StringComparison.OrdinalIgnoreCase);
        var isZero = value switch
        {
            int intValue => intValue == 0,
            _ => false
        };
        if (invert) isZero = !isZero;
        return isZero ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
