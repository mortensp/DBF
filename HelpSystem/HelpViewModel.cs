using System.IO;
using System.Windows.Media.Imaging;

namespace DBF.HelpSystem;

public class HelpViewModel
{
    public string      Title        { get; }
    public string      Text         { get; }
    public BitmapImage Image        { get; }
    public double      WindowWidth  { get; }
    public double      WindowHeight { get; }

    public HelpViewModel(HelpContent content)
    {
        Title = content.Title;
        Text  = content.Text;

        if (content.Image != null)
        {
            Image = ToBitmapImage(content.Image);

            // Brug PNG'ens størrelse som udgangspunkt
            WindowWidth  = Image.PixelWidth + 80;   // lidt margin
            WindowHeight = Image.PixelHeight + 200; // plads til tekst + margin

            // Sæt max-størrelser
            if (WindowWidth >  600)
                WindowWidth =  600;

            if (WindowHeight >  830)
                WindowHeight =  830;
        }
        else
        {
            WindowWidth  = 600;
            WindowHeight = 400;
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
