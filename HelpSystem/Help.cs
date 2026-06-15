using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DBF.HelpSystem;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DBF.HelpSystem;

public static class Help
{
    public static readonly DependencyProperty KeyProperty = 
                           DependencyProperty.RegisterAttached(
                                                                "Key"
                                                              , typeof(string)
                                                              , typeof(Help)
                                                              , new PropertyMetadata(null, OnHelpKeyChanged));

    public static void SetKey(DependencyObject obj, string value)
                        => obj.SetValue(KeyProperty, value);

    public static string GetKey(DependencyObject obj)
                        => (string)obj.GetValue(KeyProperty);

    private static void OnHelpKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            element.PreviewKeyDown-= OnPreviewKeyDown;
            element.PreviewKeyDown+= OnPreviewKeyDown;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1)
        {
            e.Handled      = true; // stop bubbling
            var    element = (DependencyObject)sender;
            string key     = GetKey(element);

            Application.Current
                       .Dispatcher
                       .BeginInvoke( new Action(() =>
                                   {
                                   HelpWindow.ShowHelp(key);
                                   }), DispatcherPriority.Background);
        }
    }
}
