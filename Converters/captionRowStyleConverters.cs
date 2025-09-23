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

    public class SecondPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var index1 = text?.IndexOf('-') ?? -1;
                var index2 = text.IndexOf(':');

                if (index2 < 0)
                    index2 = text.Length;

                return index1 <= 0
                     ? string.Empty
                     : text.Substring(index1 + 1, index2 - index1 - 1);
            }
            else
                return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ThirdPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var index = text?.IndexOf(':') ?? -1;

                if (index == -1)
                    return string.Empty;

                var index2 = text?.Substring(index).IndexOf('(') ?? -1;
                return index > 0 ? text.Substring(index, index2) : string.Empty;
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class FourthPartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tekst = value?.ToString();
            var index = tekst?.IndexOf('(') ?? -1;
            return index > 0 ? tekst.Substring(index) : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}