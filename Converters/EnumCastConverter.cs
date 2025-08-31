using System.Globalization;
using System.Windows.Data;

namespace DBF.Converters
{
    public class EnumCastConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ChangeType(value, targetType);
        }
    }
}