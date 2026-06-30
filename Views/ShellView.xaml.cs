using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.UserControls;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();

            if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState =  System.Windows.WindowState.Normal;

            Loaded += ShellView_Loaded;
            Closing+= ShellView_Closing;

            try
            {
                // Only show the test button when running under a debugger
                if (Debugger.IsAttached)
                {
                    var btn = this.FindName("mnuTest") as UIElement;

                    if (btn != null)
                        btn.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                // ignore any lookup failures
            }

             var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();

    int preference = (int)DwmInterop.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;

    DwmInterop.DwmSetWindowAttribute(
        hwnd,
        DwmInterop.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
        ref preference,
        sizeof(int));
        }

        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
          
            // restore size and position
            if (Properties.Settings.Default.WindowWidth >  0)
            {
                Width       = Properties.Settings.Default.WindowWidth;
                Height      = Properties.Settings.Default.WindowHeight;
                Left        = Properties.Settings.Default.WindowLeft;
                Top         = Properties.Settings.Default.WindowTop;
                WindowState = (WindowState)Properties.Settings.Default.WindowState;

                if (WindowState == WindowState.Minimized)
                    WindowState =  WindowState.Normal;
            }
        }

        private void ShellView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save size and position
            if (WindowState == WindowState.Normal
            ||  WindowState == WindowState.Minimized)
            {
                Properties.Settings.Default.WindowWidth  = Width;
                Properties.Settings.Default.WindowHeight = Height;
                Properties.Settings.Default.WindowLeft   = Left;
                Properties.Settings.Default.WindowTop    = Top;
                WindowState                              = WindowState.Normal;
            }

            Properties.Settings.Default.WindowState = (int)WindowState;
            Properties.Settings.Default.Save();
        }
    }
}
