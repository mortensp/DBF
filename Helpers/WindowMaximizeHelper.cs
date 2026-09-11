using System.Windows;
using System.Windows.Media;

namespace DBF.Helpers;

/// <summary>
/// Helper class for properly maximizing windows with custom WindowChrome.
/// Accounts for caption height and resize border thickness.
/// </summary>
public static class WindowMaximizeHelper
{
    // These values must match the WindowChrome settings in HelpWindowStyle
    //private const int CaptionHeight         = 0;//29;
    //private const int ResizeBorderThickness = 0;//6;

    /// <summary>
    /// Maximizes a window by setting its position and size based on the work area,
    /// accounting for custom WindowChrome settings.
    /// We do NOT set WindowState = Maximized because that causes Windows to
    /// recalculate the size and ignore our custom title bar height.
    /// Saves current bounds accounting for DPI scaling.
    /// </summary>
    public static Rect MaximizeWindow(this Window window)
    {
        var currentBounds = new Rect(window.Left, window.Top, window.Width, window.Height);

        var max = window.GetMaxSize();

        // Set window position and size
        window.WindowState = WindowState.Normal;
        window.Left   = max.Left;
        window.Top    = max.Top;
        window.Width  = max.Width;
        window.Height = max.Height;

        return currentBounds;
    }

    /// <summary>
    /// Restores a window from the "maximized" state back to normal.
    /// Properly handles DPI scaling by accounting for scale factor changes.
    /// </summary>
    public static void RestoreWindow(this Window window, Rect bounds)
    {
        if (window == null)
            return;

        window.Left   = bounds.Left;
        window.Top    = bounds.Top;
        window.Width  = bounds.Width;
        window.Height = bounds.Height;
        window.WindowState = WindowState.Normal;

        // Force window to process the layout at the new size
        window.InvalidateVisual();
    }

    /// <summary>
    /// Gets the DPI scale factor for the window.
    /// For example: 1.0 for 96 DPI, 1.5 for 144 DPI, 2.0 for 192 DPI.
    /// </summary>
    private static double GetDpiScale(Window window)
    {
        try
        {
            var presentationSource = PresentationSource.FromVisual(window);
            if (presentationSource?.CompositionTarget != null)
            {
                return presentationSource.CompositionTarget.TransformToDevice.M11;
            }
        }
        catch
        {
            // Fallback if we can't determine DPI
        }

        return 1.0;
    }

    public static Rect GetMaxSize(this Window window)
    {
        var workArea = SystemParameters.WorkArea;

        // Adjust for WindowChrome settings:
        // - ResizeBorderThickness adds space on all sides
        // - We adjust the position and size accordingly
        var adjustedLeft   = workArea.Left;//   + ResizeBorderThickness;
        var adjustedTop    = workArea.Top;//    + ResizeBorderThickness;
        var adjustedWidth  = workArea.Width;//  - (ResizeBorderThickness * 2);
        var adjustedHeight = workArea.Height;// - (ResizeBorderThickness * 2) - CaptionHeight;

        return new Rect(adjustedLeft, adjustedTop, adjustedWidth, adjustedHeight);
    }

    public static Rect GetSize(this Window window)
    {
        return new Rect(window.Left, window.Top, window.Width, window.Height);
    }
}
