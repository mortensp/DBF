//using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DBF.Converters
{
    public class GroupColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tekst = value?.ToString().ToLower();

            // Vælg farve baseret på indhold
            if (tekst?.ToLower().Contains(" rød ") == true)
                return Brushes.Red;

            if (tekst?.ToLower().Contains(" grøn ") == true)
                return Brushes.Green;

            if (tekst?.ToLower().Contains(" gul ") == true)
                return Brushes.Orange;

            if (tekst?.ToLower().Contains(" blå ") == true)
                return Brushes.Blue;

            
                return Brushes.Black; // Standardfarve
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}