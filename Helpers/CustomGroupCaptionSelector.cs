using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Cells;

namespace DBF.Helpers
{
    //public class CustomGroupCaptionSelector : DataTemplateSelector
    //{
    //    public DataTemplate ExpandedTemplate  { get; set; }
    //    public DataTemplate CollapsedTemplate { get; set; }

    //    public bool         HideWhenCollapsed { get; set; } = false;

    //    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    //    {
    //        var group = item as Group;

    //        if (group != null && (group.IsExpanded || !HideWhenCollapsed))
    //            return ExpandedTemplate;
    //        else
    //            return CollapsedTemplate;
    //    }
    //}


    public class CustomCaptionSummaryCellRenderer : GridCaptionSummaryCellRenderer
    {
        protected override void OnWireDisplayUIElement(GridCaptionSummaryCell uiElement)
        {
            base.OnWireDisplayUIElement(uiElement);
      
            var group = dataColumn.RowData as Group;

            if (group != null && !group.IsExpanded)
            {
                // Skjul cellen ved at sætte Visibility til Collapsed
                style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            }
            else
            {
                style.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Visible));
            }

            base.OnUpdateCellStyle(dataColumn, style);
        }
    }


}