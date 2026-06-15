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

namespace DBF.Views;

public enum ConfirmYesNo
{
      No
    , Yes 
}

/// <summary>
/// Interaction logic for ConfirmRestartView.xaml
/// </summary>
public partial class ConfirmRestartView : Window
{
    public ConfirmYesNo Choice { get; private set; } = ConfirmYesNo.No;

    public ConfirmRestartView(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        Choice       = ConfirmYesNo.Yes;
        DialogResult = true;
        Close();
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        Choice       = ConfirmYesNo.No;
        DialogResult = false;
        Close();
    }
}

