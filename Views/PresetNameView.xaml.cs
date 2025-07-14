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
    /// Interaction logic for PresetNameView.xaml
    /// </summary>
    public partial class PresetNameView : Window
    {
        public PresetNameView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PresetName.Focus();
        }
    }
}
