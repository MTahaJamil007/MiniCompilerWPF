using System.Globalization;
using System.Windows.Data;

namespace MiniCompilerWPF.Converters;

public class PhaseNameConverter : IValueConverter
{
    private static readonly string[] Names = ["Lex", "Parse", "Semantic", "IR", "Optimize", "Emit"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && index >= 0 && index < Names.Length) return Names[index];
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
