using System.Globalization;
using System.Windows.Data;

namespace DBF.Converters
{
    public class FirstPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tekst = value?.ToString();
            var index = tekst?.IndexOf('-') ?? -1;
            return index > 0 ? tekst.Substring(0, index+1) : tekst;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}