using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.UserControls;
using DBF.ViewModels;
using System.Diagnostics;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window, IHandleStateChanged
    {
        private ShellViewModel _viewModel;
        private Rect           _previousBounds;
        public ShellView()
        {
            InitializeComponent();

            if (WindowState == WindowState.Maximized)
                WindowState =  WindowState.Normal;

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
                                              hwnd
                                            , DwmInterop.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE
                                            , ref preference
                                            , sizeof(int));
        }

        #region Ismaximized Dependency Property
            public bool IsMaximized
            {
                get { return (bool)GetValue(IsMaximizedProperty); }
                set { SetValue(IsMaximizedProperty, value); }
            }

            public static readonly DependencyProperty IsMaximizedProperty = 
                                   DependencyProperty.Register( nameof(IsMaximized)
                                                              , typeof(bool)
                                                              , typeof(Window)
                                                              , new PropertyMetadata(false));
        #endregion

        public void ToggleMaximize(bool maximize)
        {
            if (maximize)
            {
                _previousBounds = new Rect(this.Left, this.Top, this.Width, this.Height);
                this.MaximizeWindow();
                IsMaximized = true;
                //ResizeMode = ResizeMode.NoResize;
            }
            else
            {
                this.RestoreWindow(_previousBounds);
                ResizeMode              = ResizeMode.CanResize;
                _viewModel.IsFullscreen = false;
                IsMaximized             = false;
            }
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                ToggleMaximize(true);
        }

        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel      = (ShellViewModel)this.DataContext;
            _previousBounds = new Rect(Left, Top, Width, Height);

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

            if (this.GetMaxSize() != null)
                IsMaximized = this.GetMaxSize() == this.GetSize();
        }

        private void ShellView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save size and position
            if (WindowState == WindowState.Normal
            ||  WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;

                Properties.Settings.Default.WindowWidth  = Width;
                Properties.Settings.Default.WindowHeight = Height;
                Properties.Settings.Default.WindowLeft   = Left;
                Properties.Settings.Default.WindowTop    = Top;
            }

            Properties.Settings.Default.WindowState = (int)WindowState;
            Properties.Settings.Default.Save();
        }
    }
}
