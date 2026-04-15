using Syncfusion.UI.Xaml.Grid;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for DbfMembersView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        public ControlView()
        {
            InitializeComponent();
            try
            {
                   // Only show the test button when running under a debugger
                if (Debugger.IsAttached)
                {
                    var btn = this.FindName("btnTest") as UIElement;

                    if (btn != null)
                        btn.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // ignore any lookup failures
            }
        }
    }
}
