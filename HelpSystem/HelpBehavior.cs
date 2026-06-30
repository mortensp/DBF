using System.Windows;
using System.Windows.Controls;

namespace DBF.HelpSystem;


/// <summary>
/// Provides attached properties to enable help functionality on buttons. When the "Attach" property is set to true, clicking the button will trigger the help system to display relevant help content based on the specified key or the nearest host with a help key.
/// </summary>
/// <usages>
///   <example>
///     <code>
///       <UserControl ... xmlns:help="clr-namespace:DBF.HelpSystem" help:Help.Key="MyControl.HelpKey">
///           <Button Content="?" help:HelpBehavior.Attach="True" />
///       </UserControl>
///     </code>
///   </example>
///  or
///   <example>
///     <code>
///         <Button Content="?" help:HelpBehavior.Attach="True" help:HelpBehavior.Key="Explicit.Key" />
///     </code>
///   </example>
/// </usages>
public static class HelpBehavior
{
    public static readonly DependencyProperty AttachProperty = 
                           DependencyProperty.RegisterAttached( "Attach", typeof(bool), typeof(HelpBehavior)
                                                              , new PropertyMetadata(false, OnAttachChanged));

    public static void SetAttach(DependencyObject d, bool v) => d.SetValue(AttachProperty, v);

    public static bool GetAttach(DependencyObject d) => (bool)d.GetValue(AttachProperty);

    // Optional: explicit key on the button/behavior to override nearest host
    public static readonly DependencyProperty KeyProperty = 
                           DependencyProperty.RegisterAttached( "Key", typeof(string), typeof(HelpBehavior)
                                                              , new PropertyMetadata(null));

    public static void   SetKey(DependencyObject d, string v) => d.SetValue(KeyProperty, v);

    public static string GetKey(DependencyObject d) => (string)d.GetValue(KeyProperty);

    private static void OnAttachChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Button btn)
            if ((bool)e.NewValue)
                btn.Click += Btn_Click;
            else
                btn.Click -= Btn_Click;
    }

    private static void Btn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is DependencyObject d)
        {
            var explicitKey = GetKey(d);

            if (!string.IsNullOrEmpty(explicitKey))
            {
                Help.ShowHelpByKey(explicitKey);
                return;
            }

            // Let Help search upwards for a `help:Help.Key`
            Help.ShowHelp(d);
        }
    }
}
