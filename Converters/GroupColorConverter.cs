
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DBF.Converters
{
    public class GroupColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var Text = value?.ToString().ToLower();

             // Select color based on content
          
            if (Text?.ToLower().Contains(" rød ") == true)
                return Brushes.Red;

            if (Text?.ToLower().Contains(" grøn ") == true)
                return Brushes.Green;

            if (Text?.ToLower().Contains(" gul ") == true)
                return Brushes.Orange;

            if (Text?.ToLower().Contains(" blå ") == true)
                return Brushes.Blue;

            
                return Brushes.Black; // Default color
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
