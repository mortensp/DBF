using System.Windows;

namespace DBF.Services;

public static class PrintServiceWpf
{
    public static void PrintVisual(FrameworkElement element, PrintSettings settings)
    {
        var dlg = new PrintWPF.PrintWPFWindow
                  {
                      DocumentSource = element  // fx et Grid, Canvas, UserControl
                  };

        dlg.ShowDialog();
    }
}
