using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using System.Windows.Data;

namespace DBF.Converters
{
    public class CollectionContainsToBrushConverter : IValueConverter
    {
        public Brush TrueBrush  { get; set; } = Brushes.Red;
        public Brush FalseBrush { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var coll = value as IEnumerable<string>;
            var key  = parameter as string;

            if (coll != null && !string.IsNullOrEmpty(key) && coll.Contains(key))
                return TrueBrush;

            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => throw new NotSupportedException();
    }
}
