using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;

namespace DBF;

public static class ZoomBehavior
{
    private static Configuration   configuration;
    private static FontSizeService fontSizeService;
    private static double          minFontSize;
    private static double          maxFontSize;

    #region Attached Property : EnableZoom
        public static readonly DependencyProperty EnableZoomProperty = 
                               DependencyProperty.RegisterAttached( "EnableZoom"
                                                                  , typeof(bool)
                                                                  , typeof(ZoomBehavior)
                                                                  , new PropertyMetadata(false, OnEnableZoomChanged));

        public static void SetEnableZoom(DependencyObject obj, bool value)
                                                            => obj.SetValue(EnableZoomProperty, value);

        public static bool GetEnableZoom(DependencyObject obj)
                                                            => (bool)obj.GetValue(EnableZoomProperty);

        private static void OnEnableZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            configuration   = IoC.Get<Configuration>();
            fontSizeService = IoC.Get<FontSizeService>();
            minFontSize     = Configuration.FontSizes.Min();
            maxFontSize     = Configuration.FontSizes.Max();

            if (d is not FrameworkElement fe)
                return;

            if ((bool)e.NewValue)
                fe.PreviewKeyDown += OnKeyDown;
            else
                fe.PreviewKeyDown -= OnKeyDown;
        }
    #endregion

    #region Attached Property : ZoomLevel
        public static readonly DependencyProperty ZoomLevelProperty = 
                               DependencyProperty.RegisterAttached( "ZoomLevel"
                                                                  , typeof(double)
                                                                  , typeof(ZoomBehavior)
                                                                  , new PropertyMetadata(1.0, OnZoomLevelChanged));

        public static void SetZoomLevel(DependencyObject obj, double value)
                                                            => obj.SetValue(ZoomLevelProperty, value);

        public static double GetZoomLevel(DependencyObject obj)
                                                            => (double)obj.GetValue(ZoomLevelProperty);

        private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement fe)
                return;

            double zoom = (double)e.NewValue;

            // If Window → zoom content instead
            if (fe is Window w && w.Content is FrameworkElement content)
                fe = content;

            if (fe.LayoutTransform is not ScaleTransform st)
            {
                st                 = new ScaleTransform(1.0, 1.0);
                fe.LayoutTransform = st;
            }

            st.ScaleX = zoom;
            st.ScaleY = zoom;
        }
    #endregion

    // -------------------------------
    // Ctrl + +  /  Ctrl + -
    // -------------------------------
    private static void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            return;

        if (sender is not FrameworkElement fe)
            return;

        // Window → zoom content
        if (e.Key == Key.OemPlus || e.Key == Key.Add)
        {
            e.Handled              = true;
            configuration.FontSize+= 2;
        }
        else
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                e.Handled              = true;
                configuration.FontSize-= 2;
            }

        configuration.FontSize   = Math.Max(minFontSize, configuration.FontSize);
        configuration.FontSize   = Math.Min(maxFontSize, configuration.FontSize);
        fontSizeService.FontSize = configuration.FontSize;

    }
}
