using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.Helpers
{
    //public class SfGridBehavior : Behavior<SfDataGrid>
    //{
    //    protected override void OnAttached()
    //    {
    //        ////default CaptionSummaryCellRenderer is removed.            
    //        //AssociatedObject.CellRenderers.Remove("CaptionSummary");

    //        //Customized CaptionSummaryCellRenderer is added.
    //        //AssociatedObject.CellRenderers.Add("CaptionSummary", new CustomCaptionSummaryCellRenderer());
    //    }
    //}


    public static class SfDataGridRefreshBehavior
    {
        // Bruges som i XAML: help:SfDataGridRefreshBehavior.EnableAutoRefresh="False"

        public static readonly DependencyProperty EnableAutoRefreshProperty =
            DependencyProperty.RegisterAttached(
                "EnableAutoRefresh",
                typeof(bool),
                typeof(SfDataGridRefreshBehavior),
                new PropertyMetadata(false, OnEnableAutoRefreshChanged));

        public static void SetEnableAutoRefresh(DependencyObject element, bool value)
        {
            element.SetValue(EnableAutoRefreshProperty, value);
        }

        public static bool GetEnableAutoRefresh(DependencyObject element)
        {
            return (bool)element.GetValue(EnableAutoRefreshProperty);
        }

        private static void OnEnableAutoRefreshChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SfDataGrid grid && (bool)e.NewValue)
            {
                grid.Loaded += (s, args) =>
                {
                    if (grid.ItemsSource is INotifyCollectionChanged collection)
                    {
                        collection.CollectionChanged += (cs, ce) =>
                        {
                            //grid.View?.Refresh();
                        };

                        // Cast til IEnumerable for at kunne iterere
                        //if (grid.ItemsSource is IEnumerable enumerable)
                        //{
                        //    foreach (var item in enumerable)
                        //    {
                        //        if (item is INotifyPropertyChanged npc)
                        //        {
                        //            npc.PropertyChanged += (ps, pe) =>
                        //            {
                        //                grid.View?.Refresh();
                        //            };
                        //        }
                        //    }
                        //}
                    }
                };
            }
        }
    }
}