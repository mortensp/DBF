using System.Windows;
using System.Windows.Input;
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

        var workArea = SystemParameters.WorkArea;
        var vm       = (HelpViewModel)DataContext;

        this.MaxHeight = workArea.Height - 20;
        this.MaxWidth  = workArea.Width - 20;
        this.Height    = Math.Min(vm.WindowHeight ?? this.MaxHeight, this.MaxHeight);
        this.Width     = Math.Min(vm.WindowWidth ?? this.MaxWidth, this.MaxWidth);

        // Center vinduet med hensyn til titlebar
        this.Left = (workArea.Width - this.Width) / 2 + workArea.Left;
        this.Top  = (workArea.Height - this.Height + SystemParameters.CaptionHeight/2) / 2 + workArea.Top;
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
