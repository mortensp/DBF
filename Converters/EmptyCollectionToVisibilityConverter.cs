using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DBF.Converters
{
    public class EmptyCollectionToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = true;

            if (value is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                isEmpty        = !enumerator.MoveNext();
            }
            else
                if (value == null)
                    isEmpty = true;
                else
                    isEmpty = false;

            if (Invert)
                isEmpty = !isEmpty;

            return isEmpty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
