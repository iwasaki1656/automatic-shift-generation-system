using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ShiftManagement.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool CollapseWhenInvisible { get; set; } = true;
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Invert) b = !b;
        if (b) return Visibility.Visible;
        return CollapseWhenInvisible ? Visibility.Collapsed : Visibility.Hidden;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            bool b = v == Visibility.Visible;
            return Invert ? !b : b;
        }
        return false;
    }
}

public class HexColorToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                return new SolidColorBrush(color);
            }
            catch
            {
                return Brushes.Transparent;
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ViolationToBorderBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool hasViolation && hasViolation)
        {
            return new SolidColorBrush(Colors.Red);
        }
        return new SolidColorBrush(Color.FromRgb(226, 232, 240));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
