using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DBF.Converters
{

    public class SecondPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tekst = value?.ToString();
            var index1 = tekst?.IndexOf('-') ?? -1;
            var index2 = tekst?.IndexOf(':') ?? -1;
            return index1 == 0 
                ? string.Empty
                : tekst.Substring(index1+1, index2 - index1-1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ThirdPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tekst = value?.ToString();
            var index = tekst?.IndexOf(':') ?? -1;
            return index > 0 ? tekst.Substring(index) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}