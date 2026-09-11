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

        // Ny instans af samme UserControl-type
        var clone = (FrameworkElement)Activator.CreateInstance(type);

        // Samme DataContext (samme ViewModel)
        clone.DataContext = original.DataContext;

        return clone;
    }
}
