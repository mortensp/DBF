using System.Windows;
using System.Windows.Controls;
using Microsoft.DotNet.DesignTools.ViewModels;

namespace DBF.Services.Print;

public static class PrintService
{
    /// <summary>
    /// Prints the specified control using the system print dialog.
    /// </summary>
    /// <param name="control"></param>
    /// <remarks>
    /// Use like this:
    ///     private void PrintButton_Click(object sender, RoutedEventArgs e)
    ///     {
    ///         private  var view = new PrintableView
    ///                         {
    ///                             DataContext = ViewModel
    ///                         };
    ///         PrintService.PrintControl(view);
    ///     }
    /// </remarks>
    public static void PrintControl(FrameworkElement control)
    {
        var dialog = new System.Windows.Controls.PrintDialog();

        if (dialog.ShowDialog() != true)
            return;

        var pageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        var margin   = new Thickness(40);

        var paginator = new VisualPaginator(control, pageSize, margin);

        dialog.PrintDocument(paginator, "Print");
    }
}
