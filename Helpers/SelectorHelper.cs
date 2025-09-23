using System.Windows;

namespace DBF.Helpers
{
    public static class SelectorHelper
    {
        public static readonly DependencyProperty HideWhenCollapsedProperty =
            DependencyProperty.RegisterAttached("HideWhenCollapsed", typeof(bool), typeof(SelectorHelper), new PropertyMetadata(false));

        public static bool GetHideWhenCollapsed(DependencyObject obj) => (bool)obj.GetValue(HideWhenCollapsedProperty);
        public static void SetHideWhenCollapsed(DependencyObject obj, bool value) => obj.SetValue(HideWhenCollapsedProperty, value);
    }

}