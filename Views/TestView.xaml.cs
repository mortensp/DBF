using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DBF.DataModel;
using DBF.Helpers;
using PropertyChanged;
using Configuration = DBF.DataModel.Configuration;

namespace DBF.Views;
  
public partial class TestView : Window
{
    public TestView()
    {
        InitializeComponent();

        DataContext = this;

        // Only create/load runtime data when not in designer
        if (Design.IsInDesignMode(this))
        {
            Configuration.Load();
        }
            BridgeTimer = new() { BackgroundColor = (Color)ColorConverter.ConvertFromString("#F2460D") };
        
    }

    public Configuration Configuration { get; set; } = new();
    public BridgeTimer BridgeTimer { get; set; }
}
