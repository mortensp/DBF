using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Syncfusion.UI.Xaml.Grid;

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
            //try
            //{
            //       // Only show the test button when running under a debugger
            //    if (Debugger.IsAttached)
            //    {
            //        var btn = this.FindName("btnTest") as UIElement;

            //        if (btn != null)
            //            btn.Visibility = Visibility.Visible;
            //    }
            //}
            //catch
            //{
            //    // ignore any lookup failures
            //}
        }

        /// <summary>
        /// Activates the window that contains the control when the control receives a mouse-down event.
        /// </summary>
        /// <remarks>Locates the containing Window using Window.GetWindow and calls Activate if a window
        /// is found.</remarks>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The MouseButtonEventArgs that identifies the mouse button and event details.</param>
       ///<usages>
       ///   <UserControl ..
       ///       MouseDown="UserControl_MouseDown"/>
       ///</usages>
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Window.GetWindow(this)?.Activate();
        }
    }
}
