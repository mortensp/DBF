using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DBF.HelpSystem;
using DBF.ViewModels;

namespace DBF.Views;

/// <summary>
/// Interaction logic for HelpWindow.xaml
/// </summary>
public partial class HelpView : Window
{
    public HelpView()
    {
        InitializeComponent();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // The window is now fully rendered and measured
        this.Height    = ((HelpViewModel)DataContext).WindowHeight;
        this.Top       = 10;
        this.MaxHeight = SystemParameters.PrimaryScreenHeight - 20;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.Focus(); // Make sure the window captures keyboard input
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (!ctrl)
            return;

        if (e.Key == Key.OemPlus || e.Key == Key.Add)
            e.Handled = true;
        else
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
                e.Handled = true;
    }
}
