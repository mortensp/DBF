using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using DBF.UserControls;

namespace DBF.Helpers;

public static class ToastService
{
    public static void Show(Screen screen, string message)
    {
        var view = screen.GetView() as DependencyObject;

        if (view == null)
            return;

        // Hvis view’et er et Window
        if (view is Window win)
        {
            Show(win, message);
            return;
        }

        // Hvis view’et er et UserControl
        if (view is ContentControl cc)
        {
            Show(cc, message);
            return;
        }
    }

    public static void Show(ContentControl window, string message)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => Show(window, message));
            return;
        }

        var toast = new Toast(message);

        var host = window.FindName("ToastHost") as Panel;
        host?.Children.Add(toast);

        toast.HorizontalAlignment = HorizontalAlignment.Right;
        toast.VerticalAlignment   = VerticalAlignment.Top;
        toast.Margin              = new Thickness(0, 20, 20, 0);
    }

    public static void Show(Window window, string message)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.Invoke(() => Show(window, message));
            return;
        }

        var toast = new Toast(message);

        var host = window.FindName("ToastHost") as Panel;
        host?.Children.Add(toast);

        toast.HorizontalAlignment = HorizontalAlignment.Right;
        toast.VerticalAlignment   = VerticalAlignment.Top;
        toast.Margin              = new Thickness(0, 20, 20, 0);
    }
}
