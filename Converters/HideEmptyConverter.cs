using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DBF.Converters
{
    public class HideEmptyConverter : IValueConverter
    {
        public bool Invert { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is null || value is string str && string.IsNullOrEmpty(str))
                 ? Visibility.Collapsed
                 : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}