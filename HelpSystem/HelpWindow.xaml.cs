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
using System.Windows.Shapes;
using DBF.ViewModels;

namespace DBF.HelpSystem;

/// <summary>
/// Interaction logic for HelpWindow.xaml
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow(HelpContent content)
    {
        InitializeComponent();
        DataContext = new HelpViewModel(content);
    }

    public static void ShowHelp(string key)
    {
        var content = HelpProvider.Get(key);
        var win     = new HelpWindow(content);
        win.ShowDialog();
    }
}

