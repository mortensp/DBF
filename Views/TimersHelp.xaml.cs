using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore.Query;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for TimersHelpWindow.xaml
    /// </summary>
    public partial class TimersHelpWindow : Window
    {
        public TimersHelpWindow()
        {
            InitializeComponent();

            // Set window height relative to screen height
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var screenWidth  = SystemParameters.PrimaryScreenWidth;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Height                = screenHeight - 100;
            Width                 = screenWidth - 100;

            helpImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/guideTilUr.jpg"));
        }

        public static BitmapImage ToBitmapImage(Bitmap bmp)
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
}
