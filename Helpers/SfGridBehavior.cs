using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using DBF.UserControls;
using Microsoft.Xaml.Behaviors;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.Helpers
{
    public class SfGridBehavior : Behavior<SfDataGrid>
    {
        protected override void OnAttached()
        {
            ////default CaptionSummaryCellRenderer is removed.            
            AssociatedObject.CellRenderers.Remove("CaptionSummary");

            //Customized CaptionSummaryCellRenderer is added.
            AssociatedObject.CellRenderers.Add("CaptionSummary", new CustomCaptionSummaryCellRenderer());
        }
    }
}

namespace DBF.Helpers
{

}