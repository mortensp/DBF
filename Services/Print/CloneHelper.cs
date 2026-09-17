using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace DBF.Services;

public static class CloneHelper
{
    public static FrameworkElement CloneElement(FrameworkElement original)
    {
        if (original is null)
            return null;

        var type = original.GetType();

        // New instance of the same UserControl type
        var clone = (FrameworkElement)Activator.CreateInstance(type);

        // Same DataContext (same ViewModel)
        clone.DataContext = original.DataContext;

        return clone;
    }
}
