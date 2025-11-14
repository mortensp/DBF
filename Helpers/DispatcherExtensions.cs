using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.Helpers
{
    public static class DispatcherExtensions
    {
        public static void SafeRefreshSorting(this SfDataGrid grid)
        {
            if (grid.Dispatcher.CheckAccess())
                grid.RefreshSorting();
            else
                grid.Dispatcher.Invoke(() => grid.RefreshSorting());
        }

        private static void RefreshSorting(this SfDataGrid grid)
        {
            var sortDescriptions = grid.SortColumnDescriptions.ToList();
            grid.SortColumnDescriptions.Clear();

            foreach (var sortDesc in sortDescriptions)
            {
                grid.SortColumnDescriptions.Add(sortDesc);
            }
        }
    }
}