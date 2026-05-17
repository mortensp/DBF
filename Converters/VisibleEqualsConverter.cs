using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace DBF.Converters;
public class VisibleEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString()
            ? Visibility.Visible
            : Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
