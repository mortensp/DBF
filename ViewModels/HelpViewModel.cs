using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DBF.HelpSystem;

namespace DBF.ViewModels;

public class HelpViewModel : PropertyChangedBase
{
    private const double MaxWidth  = 950;
    private const double MaxHeight = 823;
    private string      _key;

    public string      Title        { get; private set; }
    public string      Text         { get; private set; }

    public BitmapImage Image        { get; private set; }

    public double      WindowWidth  { get;  set; }

    public double      WindowHeight { get;  set; }

    public string Key
    {
        get => _key;
        internal set
        {
            if (Set(ref _key, value))
            {
                var content = HelpSystem.HelpProvider.Get(_key);
                Title       = content.Title;
                Text        = content.Text;
                Image       = ToBitmapImage(content.Image);

                if (content.Image is null)
                {
                    WindowWidth  = 600;
                    WindowHeight = 400;
                }
                else
                {
                    Image = ToBitmapImage(content.Image);

                    double imgW = Image.PixelWidth;
                    double imgH = Image.PixelHeight;

                    // Aspect ratio
                    double ratio = imgW / imgH;

                    // Start with original size
                    double targetW = imgW ;
                    double targetH = imgH ;

                    // Scale down if necessary
                    double scale = 0.9;

                    if (targetW >  MaxWidth)
                        scale *= MaxWidth / targetW;

                    // Use scaling
                    WindowWidth  = targetW * scale;
                    WindowHeight = Math.Min(targetH * scale, MaxHeight);
                }
            }
        }
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
