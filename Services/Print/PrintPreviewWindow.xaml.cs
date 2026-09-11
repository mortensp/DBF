using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace DBF.Services.Print;

public partial class PrintPreviewWindow : Window
{
    private readonly FixedDocumentSequence _document;
    private readonly DocumentPaginator     _sourcePaginator;

    private PrintQueue?  _printQueue;
    private PrintTicket? _printTicket;

    private DocumentPaginator? _printPaginator;

    public PrintPreviewWindow( FixedDocumentSequence document
                             , DocumentPaginator documentPaginator)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(documentPaginator);

        InitializeComponent();

        _document        = document;
        _sourcePaginator = documentPaginator;

        DocumentViewer.Document = _document;

        Loaded     += (_, _) => FitPage();
        SizeChanged+= (_, _) => FitPage();
    }

    private void Printer_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();

        // If a printer has already been selected,
        // use it as the initial printer.
        if (_printQueue != null)
            dialog.PrintQueue = _printQueue;

        if (_printTicket != null)
            dialog.PrintTicket = _printTicket;

        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        _printQueue  = dialog.PrintQueue;
        _printTicket = dialog.PrintTicket;

        PrinterText.Text = _printQueue.FullName;

        CreatePrintPaginator();

        // Show the printer-adjusted paginator in the preview.
        DocumentViewer.Document = 
        new PaginatorDocument(_printPaginator!);

        FitPage();
    }

    private void CreatePrintPaginator()
    {
        if (_printQueue == null || _printTicket == null)
            return;

        double pageWidth;
        double pageHeight;

        GetPageSize( _printQueue
                   , _printTicket
                   , out pageWidth
                   , out pageHeight);

        _printPaginator = new ScalingDocumentPaginator(
                                                        _sourcePaginator
                                                      , new Size(pageWidth, pageHeight));
    }

    private static void GetPageSize( PrintQueue printQueue
                                   , PrintTicket printTicket
                                   , out double width
                                   , out double height)
    {
        // First try the media size from the print ticket.
        var mediaSize = printTicket.PageMediaSize;

        if (mediaSize?.Width.HasValue == true && 
            mediaSize.Height.HasValue == true)
        {
            width  = mediaSize.Width.Value;
            height = mediaSize.Height.Value;
        }
        else
        {
            // Fall back to the printer's printable area.
            width = printQueue.GetPrintCapabilities(printTicket)
                              .OrientedPageMediaWidth ?? 793d;

            height = printQueue.GetPrintCapabilities(printTicket)
                               .OrientedPageMediaHeight ?? 1122d;
        }

        // Respect landscape orientation.
        if (printTicket.PageOrientation == PageOrientation.Landscape)
        {
            if (height >  width)
                (width, height) = (height, width);
        }
        else
        {
            if (width >  height)
                (width, height) = (height, width);
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (_printQueue == null || _printTicket == null)
        {
            // No printer selected yet.
            Printer_Click(sender, e);

            if (_printQueue == null)
                return;
        }

        CreatePrintPaginator();

        if (_printPaginator == null)
            return;

        var dialog = new PrintDialog
                     {
                         PrintQueue  = _printQueue
                       , PrintTicket = _printTicket
                     };

        dialog.PrintDocument( _printPaginator
                            , "Dokument");
    }

    private void FitPage_Click(object sender, RoutedEventArgs e)
    {
        FitPage();
    }

    private void ActualSize_Click(object sender, RoutedEventArgs e)
    {
        DocumentViewer.Zoom = 100;
    }

    private void FitPage()
    {
        if (DocumentViewer.Document == null)
            return;

        if (DocumentViewer.ActualWidth  <= 0 || 
            DocumentViewer.ActualHeight <= 0)
            return;

        Size pageSize = 
             DocumentViewer.Document.DocumentPaginator.PageSize;

        if (pageSize.Width  <= 0 || 
            pageSize.Height <= 0)
            return;

        const double padding = 40;

        double availableWidth = 
               Math.Max(1, DocumentViewer.ActualWidth - padding);

        double availableHeight = 
               Math.Max(1, DocumentViewer.ActualHeight - padding);

        double scaleX = 
               availableWidth / pageSize.Width;

        double scaleY = 
               availableHeight / pageSize.Height;

        double scale = Math.Min(scaleX, scaleY);

        DocumentViewer.Zoom = 
        Math.Clamp(scale * 100.0, 10.0, 500.0);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
