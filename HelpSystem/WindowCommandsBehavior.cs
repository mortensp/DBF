using System.Windows;

namespace DBF.HelpSystem;

public static class WindowCommandsBehavior
{
    #region MinimizeCommand Dependency Property
        public static readonly DependencyProperty MinimizeCommandProperty = 
                               DependencyProperty.RegisterAttached( "MinimizeCommand", typeof(bool), typeof(WindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnMinimizeCommandChanged));

        public static void SetMinimizeCommand(DependencyObject d, bool v) => d.SetValue(MinimizeCommandProperty, v);

        public static bool GetMinimizeCommand(DependencyObject d) => (bool)d.GetValue(MinimizeCommandProperty);

        private static void OnMinimizeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var window = Window.GetWindow(btn);

                    if (window != null)
                        window.WindowState = WindowState.Minimized;
                };
        }
    #endregion

    #region MaximizeCommand Dependency Property
        public static readonly DependencyProperty MaximizeCommandProperty = 
                               DependencyProperty.RegisterAttached( "MaximizeCommand", typeof(bool), typeof(WindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnMaximizeCommandChanged));

        public static void SetMaximizeCommand(DependencyObject d, bool v) => d.SetValue(MaximizeCommandProperty, v);

        public static bool GetMaximizeCommand(DependencyObject d) => (bool)d.GetValue(MaximizeCommandProperty);

        private static void OnMaximizeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var window = Window.GetWindow(btn);

                    if (window != null)
                        window.WindowState = (window.WindowState == WindowState.Maximized)
                                           ? WindowState.Normal
                                           : WindowState.Maximized;
                };
        }
    #endregion

    #region CloseCommand Dependency Property
        public static readonly DependencyProperty CloseCommandProperty = 
                               DependencyProperty.RegisterAttached( "CloseCommand", typeof(bool), typeof(WindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnCloseCommandChanged));

        public static void SetCloseCommand(DependencyObject d, bool v) => d.SetValue(CloseCommandProperty, v);

        public static bool GetCloseCommand(DependencyObject d) => (bool)d.GetValue(CloseCommandProperty);

        private static void OnCloseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var window = Window.GetWindow(btn);
                    window?.Close();
                };
        }
    #endregion
}
