using System.ComponentModel;
using System.Windows;

namespace DBF.Helpers;

public static class Design
{
    public static bool IsInDesignMode() => (bool)DesignerProperties.IsInDesignModeProperty
                                                                   .GetMetadata(typeof(DependencyObject))
                                                                   .DefaultValue;

    public static bool IsInDesignMode(this DependencyObject dep) => (bool)DesignerProperties.GetIsInDesignMode(dep);
}

