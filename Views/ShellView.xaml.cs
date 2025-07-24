using DBF.ViewModels;
using Caliburn.Micro;
using System.Windows;
using System.Windows.Media;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();

            if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState =  System.Windows.WindowState.Normal;

            Loaded  += ShellView_Loaded;
            Closing += ShellView_Closing;
        }

        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            // Genskab størrelse og position
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
            // Gem størrelse og position
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
