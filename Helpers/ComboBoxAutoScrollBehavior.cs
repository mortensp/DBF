using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DBF.Helpers;

public static class ComboBoxAutoScrollBehavior
{
    public static readonly DependencyProperty AutoScrollProperty = 
                           DependencyProperty.RegisterAttached( "AutoScroll"
                                                              , typeof(bool)
                                                              , typeof(ComboBoxAutoScrollBehavior)
                                                              , new PropertyMetadata(false, OnAutoScrollChanged));

    public static void SetAutoScroll(DependencyObject obj, bool value)
            => obj.SetValue(AutoScrollProperty, value);

    public static bool GetAutoScroll(DependencyObject obj)
            => (bool)obj.GetValue(AutoScrollProperty);

    private static void OnAutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
            return;

        if ((bool)e.NewValue)
            combo.SelectionChanged += Combo_SelectionChanged;
        else
            combo.SelectionChanged -= Combo_SelectionChanged;
    }

    private static void Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.SelectedItem == null)
            return;

        // Wait for the event to finish and layout is updated (especially when the dropdown is just opened)
        combo.Dispatcher.BeginInvoke( new Action(() =>
                                    {
                                        var container = combo.ItemContainerGenerator.ContainerFromItem(combo.SelectedItem) as FrameworkElement;
                                    container?.BringIntoView();
                                    }), System.Windows.Threading.DispatcherPriority.Background);
    }
}

