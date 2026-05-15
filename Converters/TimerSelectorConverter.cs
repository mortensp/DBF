using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using DBF.DataModel;

namespace DBF.Converters
{
    public class TimerSelectorConverter : IMultiValueConverter
    {
        // ConverterParameter is the slot index 0..3 for the control position.
        // For Horizontal: mapping = [0,1,2,3]
        // For Vertical:   mapping = [0,2,1,3]
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return null;

            var timers = values[0] as System.Collections.ObjectModel.ObservableCollection<BridgeTimer>;
            var orientation = values[1] is Orientation o ? o : Orientation.Horizontal;

            if (timers == null)
                return null;

            if (!int.TryParse(parameter?.ToString() ?? "0", out int slot))
                slot = 0;

            int[] mappingHorizontal = { 0, 1, 2, 3 };
            int[] mappingVertical   = { 0, 2, 1, 3 };

            int index = orientation == Orientation.Horizontal ? mappingHorizontal[slot] : mappingVertical[slot];

            if (index < 0 || index >= timers.Count)
                return null;

            return timers[index];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
