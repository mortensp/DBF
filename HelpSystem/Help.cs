using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.ViewModels;

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
        if (e.Key != System.Windows.Input.Key.F1)
            return;

        e.Handled = true;

        // Find the element that actually has keyboard focus
        var focused = Keyboard.FocusedElement as DependencyObject;

        // Fall back to OriginalSource if nothing focused
        var start = focused ?? (e.OriginalSource as DependencyObject);

        var host = FindHelpHost(start);

        if (host == null)
            return;

        string key = GetKey(host);
        Application.Current.Dispatcher.InvokeAsync(async () => await showHelpAsync(key)
                                                  , DispatcherPriority.Background);
    }

    public static void ShowHelp(DependencyObject source)
    {
        // if there is no source specified, try the main window as a fallback
        var start = source ?? Application.Current?.MainWindow;
        var host  = FindHelpHost(start);

        if (host == null)
            return;

        string key = GetKey(host);
        Application.Current.Dispatcher.InvokeAsync(async () => await showHelpAsync(key)
                                                  , DispatcherPriority.Background);
    }

    // New helper to show help directly by key
    public static void ShowHelpByKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        Application.Current?.Dispatcher.InvokeAsync(async () => await showHelpAsync(key),
                                                   DispatcherPriority.Background);
    }

    private static DependencyObject FindHelpHost(DependencyObject start)
    {
        while (start != null)
        {
            var k = GetKey(start);

            if (!string.IsNullOrEmpty(k))
                return start;

            // Try visual parent first
            start = System.Windows.Media.VisualTreeHelper.GetParent(start);

            // If still null, try logical parent
            if (start == null && start is FrameworkElement fe)
                start = System.Windows.LogicalTreeHelper.GetParent(fe);
        }

        return null;
    }

    public static async Task showHelpAsync(string key)
    {
        try
        {
            var viewModel     = IoC.Get<HelpViewModel>();
            var windowManager =IoC.Get<IWindowManager>();

            viewModel.Key = key;

            await windowManager.ShowWindowAsync(viewModel);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}
