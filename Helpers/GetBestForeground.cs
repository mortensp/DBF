namespace DBF.Helpers;

public static class ForegroundHelper
{
    /// <summary>
    /// Find the best foreground color (black or white) for a given background color based on its luminance.
    /// </summary>
    /// <param name="backgroundColor"></param>
    /// <returns></returns>
    public static Color GetBestForegroundColor(this Color backgroundColor)
    {
        if (backgroundColor == null)
            return Colors.Black;

        double luminance = GetLuminance(backgroundColor);

        return luminance <  0.5 ? Colors.White : Colors.Black;
    }

    /// <summary>
    /// Find the best foreground brush (black or white) for a given background brush based on its luminance.
    /// </summary>
    /// <param name="background"></param>
    /// <returns></returns>
    public static Brush GetBestForeground(this Brush background)
    {
        if (background == null)
            return Brushes.Black;

        Color  bg        = ExtractColor(background);
        double luminance = GetLuminance(bg);

        return luminance <  0.5 ? Brushes.White : Brushes.Black;
    }

    private static Color ExtractColor(Brush brush)
    {
        switch (brush)
        {
            case SolidColorBrush scb:
                return scb.Color;

            case GradientBrush gb:
                var stops = gb.GradientStops;
                return stops[stops.Count / 2].Color;

            default:
                return Colors.White;
        }
    }

    private static double GetLuminance(Color c)
    {
        double r = c.R / 255.0;
        double g = c.G / 255.0;
        double b = c.B / 255.0;

        r = (r <= 0.03928) ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        g = (g <= 0.03928) ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        b = (b <= 0.03928) ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}
