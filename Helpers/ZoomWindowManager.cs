using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;

namespace DBF.Helpers;

public class ZoomWindowManager : WindowManager
{
    private static readonly double step = 0.05;

    protected override async Task<Window> CreateWindowAsync(object rootModel, bool isDialog, object context, IDictionary<string, object> settings)
    {
        // Let Caliburn create the window first
        var window = await base.CreateWindowAsync(rootModel, isDialog, context, settings);

        // Enable zoom on the window root
        ZoomBehavior.SetEnableZoom(window, true);

        window.Closed+= (s, e) =>
        {
            ZoomBehavior.SetEnableZoom(window, false);
        };

        return window;
    }

    public static class ZoomBehavior
    {
        #region Attached Property : EnableZoom
            public static readonly DependencyProperty EnableZoomProperty = 
                                   DependencyProperty.RegisterAttached(
                                                                        "EnableZoom"
                                                                      , typeof(bool)
                                                                      , typeof(ZoomBehavior)
                                                                      , new PropertyMetadata(false, OnEnableZoomChanged));

            public static void SetEnableZoom(DependencyObject obj, bool value)
                                                                            => obj.SetValue(EnableZoomProperty, value);

            public static bool GetEnableZoom(DependencyObject obj)
                                                                            => (bool)obj.GetValue(EnableZoomProperty);

            private static void OnEnableZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                //if (d is FrameworkElement fe)
                //{
                //    System.Diagnostics.Debug.WriteLine($"ZoomBehavior attached to: {fe.GetType().Name}");

                //    if ((bool)e.NewValue)
                //        fe.PreviewMouseWheel += OnMouseWheel;
                //    else
                //        fe.PreviewMouseWheel -= OnMouseWheel;
                //}
                if (d is FrameworkElement fe)
                    if ((bool)e.NewValue)
                        fe.PreviewKeyDown += OnKeyDown;
                    else
                        fe.PreviewKeyDown -= OnKeyDown;
            }
        #endregion

        #region Attached Property : ZoomLevel
            // tillader : <tag local:ZoomBehavior.ZoomLevel="{Binding GlobalZoom, Mode=TwoWay}" />
            public static readonly DependencyProperty ZoomLevelProperty = 
                                   DependencyProperty.RegisterAttached(
                                                                        "ZoomLevel"
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

        private static void OnKeyDown(object sender, KeyEventArgs e)
        {
            // Only zoom when Ctrl is held
            if (!( Keyboard.IsKeyDown(Key.LeftCtrl)
               ||  Keyboard.IsKeyDown(Key.RightCtrl)))
                return;

            if (sender is not FrameworkElement fe)
                return;

            // If sender is a Window, zoom its content instead
            if (fe is Window w && w.Content is FrameworkElement content)
                fe = content;

            // Ensure LayoutTransform exists
            if (fe.LayoutTransform is not ScaleTransform st)
            {
                st                 = new ScaleTransform(1.0, 1.0);
                fe.LayoutTransform = st;
            }

            double zoom = st.ScaleX;

            // Ctrl + +  (Zoom in)
            if (e.Key == Key.OemPlus || e.Key == Key.Add)
            {
                zoom     += step;
                e.Handled = true;
            }

            // Ctrl + -  (Zoom out)
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                zoom     -= step;
                e.Handled = true;
            }

            zoom = Math.Max(0.5, Math.Min(3.0, zoom));

            st.ScaleX = zoom;
            st.ScaleY = zoom;

            SetZoomLevel(fe, zoom);
        }
    }
}

