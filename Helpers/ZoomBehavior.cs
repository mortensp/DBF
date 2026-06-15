using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DBF;

public static class ZoomBehavior
{
    public static readonly DependencyProperty EnableZoomProperty =
        DependencyProperty.RegisterAttached(
            "EnableZoom",
            typeof(bool),
            typeof(ZoomBehavior),
            new PropertyMetadata(false, OnEnableZoomChanged));

    public static void SetEnableZoom(DependencyObject obj, bool value)
        => obj.SetValue(EnableZoomProperty, value);

    public static bool GetEnableZoom(DependencyObject obj)
        => (bool)obj.GetValue(EnableZoomProperty);

    private static void OnEnableZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement fe)
        {
            if ((bool)e.NewValue)
                fe.PreviewMouseWheel += OnMouseWheel;
            else
                fe.PreviewMouseWheel -= OnMouseWheel;
        }
    }

    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Find or create ScaleTransform
                if (fe.LayoutTransform is not ScaleTransform st)
                {
                    st = new ScaleTransform(1.0, 1.0);
                    fe.LayoutTransform = st;
                }

                double zoom = st.ScaleX;
                zoom += e.Delta > 0 ? 0.1 : -0.1;
                zoom = Math.Max(0.5, Math.Min(2.5, zoom));

                st.ScaleX = zoom;
                st.ScaleY = zoom;

                e.Handled = true;
            }
        }
    }
}

