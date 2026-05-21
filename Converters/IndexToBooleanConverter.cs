using System.Globalization;
using System.Windows.Data;

namespace MiniCompilerWPF.Converters;

public class IndexToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return int.TryParse(parameter.ToString(), out var index) && value is int current && current == index;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter == null) return Binding.DoNothing;
        return int.TryParse(parameter.ToString(), out var index) ? index : Binding.DoNothing;
    }
}
