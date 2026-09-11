using System.Windows;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Grid;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace DBF.Services;

public static class PrintService2
{
    public static void Print(FrameworkElement element, PrintSettings settings)
    {
        var dlg = new PrintDialog();

        if (dlg.ShowDialog() != true)
            return;

        var paginator = new VisualPaginator(element, settings);

        dlg.PrintDocument(paginator, "Print");
    }

    public static void Preview(FrameworkElement element, PrintSettings settings)
    {
        try
        {
            var paginator = new VisualPaginator(element, settings);

            var window = new Window
                         {
                             Title                 = "Print Preview"
                           , Width                 = 800
                           , Height                = 600
                           , WindowStartupLocation = WindowStartupLocation.CenterOwner
                         };

            var doc = new PaginatorDocument(paginator);

            var dv = new DocumentViewer
                     {
                         Document = doc.ToFixedDocumentSequence()
                     };

            window.Content = dv;
            window.Owner   = Application.Current.MainWindow;
            window.ShowDialog();
        }

        catch (Exception ex)
        {
            throw ex;
        }
    }
}
