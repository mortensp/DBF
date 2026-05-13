using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows.Data;

namespace DBF.Converters;



public class CapitalizerConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {

            if (string.IsNullOrEmpty(str))
                return str;

            // Capitalize first letter only
            return char.ToUpper(str[0]) + str.Substring(1);
        }


        if (value is CultureInfo ci)
        {
            var name = ci.NativeName;

            if (string.IsNullOrEmpty(name))
                return name;

            // Capitalize first letter only
            return char.ToUpper(name[0], ci) + name.Substring(1);
        }



        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
