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

        // Nu er vinduet 100% tegnet og målt
        this.Height    = ((HelpViewModel)DataContext).WindowHeight;
        this.Top       = 10;
        this.MaxHeight = SystemParameters.PrimaryScreenHeight - 20;
        //CenterWindowOnScreen();
    }

    //public void CenterWindowOnScreen()
    //{
    //    var screenWidth  = SystemParameters.PrimaryScreenWidth;
    //    var screenHeight = SystemParameters.PrimaryScreenHeight;

    //    this.Left = (screenWidth - this.Width) / 2;
    //    this.Top  = (screenHeight - this.Height) / 2;
    //}
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.Focus(); // så vinduet fanger tastaturet
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        bool ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        if (!ctrl)
            return;

        //const double step = 10;

        if (e.Key == Key.OemPlus || e.Key == Key.Add)
        {
            //if (DataContext is HelpViewModel vm)
            //{
            //    vm.WindowWidth+= step;
            //    vm.NotifyOfPropertyChange(nameof(vm.WindowWidth));
            //}

            e.Handled = true;
        }
        else
            if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
            {
                //if (DataContext is HelpViewModel vm)
                //{
                //    vm.WindowWidth = Math.Max(300, vm.WindowWidth - step);
                //    vm.NotifyOfPropertyChange(nameof(vm.WindowWidth));
                //}

                e.Handled = true;
            }
    }
}
