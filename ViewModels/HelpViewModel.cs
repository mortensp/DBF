using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DBF.Helpers;
using DBF.HelpSystem;

namespace DBF.ViewModels;

public class HelpViewModel : PropertyChangedBase
{
    private const double MaxWidth               = 950;
    private const double MaxHeight              = 823;
    private const double DefaultPhysicalWidthCm = 20.0;
    private const double DipPerInch             = 96.0;
    private const double CmPerInch              = 2.54;
    private       string _key;

    public string      Title        { get; private set; }
    public string      Text         { get; private set; }
    public BitmapImage Image        { get; private set; }
    public double?     WindowWidth  { get; set; }
    public double?     WindowHeight { get; set; }

    public string Key
    {
        get => _key;
        internal set
        {
            if (Set(ref _key, value))
            {
                var content = HelpSystem.HelpProvider.Get(_key);

                if (content.IsMissing)
                    Title = $"{LocHelp.Unknown_Help_Title}: Key: {_key}";
                else
                    Title = content.Title;

                Text  = content.Text;
                Image = ToBitmapImage(content.Image);

                if (content.Image is null
                ||  content.Width is not null)
                {
                    WindowWidth  = content.Width;
                    WindowHeight = content.Height;
                }
                else
                {
                    //Image = ToBitmapImage(content.Image);

                    // Compute optimal size based on image DPI/size, fall back to default physical size
                    var (w, h)   = ComputeOptimalSizeFromImage(Image, DefaultPhysicalWidthCm);
                    WindowWidth  = w;
                    WindowHeight = h;
                }
            }
        }
    }

    private (double width, double height) ComputeOptimalSizeFromImage(BitmapImage img, double defaultCm)
    {
        if (img == null)
            return (Math.Min(MaxWidth, 400), Math.Min(MaxHeight, 600));

        double pixelW = img.PixelWidth;
        double pixelH = img.PixelHeight;
        double dpiX   = img.DpiX >  0 ? img.DpiX : 0;
        double dpiY   = img.DpiY >  0 ? img.DpiY : 0;

        // Determine a reasonable physical width in cm using image DPI if available.
        double physicalWidthCm = 0;

        if (dpiX >  0)
            physicalWidthCm = pixelW / dpiX * CmPerInch;

        // Use image physical width if it's reasonable, otherwise fallback to defaultCm
        bool   physicalLooksValid = physicalWidthCm >  0 && Math.Abs(physicalWidthCm - defaultCm) / defaultCm <  0.5;
        double targetWidthCm      = physicalLooksValid ? physicalWidthCm : defaultCm;

        // Convert cm to DIP (WPF device-independent pixels)
        double targetWidthDip = targetWidthCm / CmPerInch * DipPerInch;

        // Respect max constraints preserving aspect ratio
        double ratio   = pixelW >  0 && pixelH >  0 ? pixelW / pixelH : 1.0;
        double targetW = Math.Min(MaxWidth, targetWidthDip);
        double targetH = targetW / ratio;

        if (targetH >  MaxHeight)
        {
            targetH = MaxHeight;
            targetW = targetH * ratio;
        }

        // If image DPI was missing and computed size is tiny/large, fallback to pixel-based scaling
        if (dpiX == 0 && (targetW <= 0 || targetH <= 0))
        {
            double scale = 0.9;
            targetW      = Math.Min(MaxWidth, pixelW * scale);
            targetH      = Math.Min(MaxHeight, pixelH * scale);
        }

        return (targetW-20, targetH);
    }

    private BitmapImage ToBitmapImage(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;

        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption  = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
