using System;
using System.Collections.Generic;
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

            // Sæt vinduets højde relativ til skærmens 
            var screenHeight      = SystemParameters.PrimaryScreenHeight;
            Height                = screenHeight - 100;
            Width                 = 200 + Height / 2;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            helpImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/guideTilUr.jpg"));
        }
    }
}
