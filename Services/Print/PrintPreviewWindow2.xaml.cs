using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using DBF.Helpers;

namespace DBF.Services.Print;

public partial class PrintPreviewWindow2 : Window
{
    // Save references to the original UI elements so we can render them again
    private FrameworkElement _headerEl;
    private FrameworkElement _centralEl;
    private FrameworkElement _footerEl;
    private double           _contentHeaderHeight;

    public PrintPreviewWindow2(FrameworkElement header, FrameworkElement central, FrameworkElement footer, double headerHeight)
    {
        InitializeComponent();

        _headerEl            = header;
        _centralEl           = central;
        _footerEl            = footer;
        _contentHeaderHeight = headerHeight;

        // Run first preview generation  
        UpdatePreview();

        this.Loaded+= (s, e) => HidePrintButton();
    }

    private void HidePrintButton()
    {
        // Find all buttons in the document viewer
        var buttons = FindVisualChildren<Button>(MyDocumentViewer);

        foreach (var button in buttons)
            if (button.Name == "PrintButton"
            ||  button.Name == "CopyButton")
                button.Visibility = Visibility.Collapsed;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var printDlg = new System.Windows.Controls.PrintDialog();

        // 1. Prepare printer's PrintTicket based on your ComboBox choices
        try
        {
            // Fetch printer's default settings (or create a new one if it is null)
            PrintTicket ticket = printDlg.PrintTicket ?? new PrintTicket();

            // Synkroniser orientering [1]
            if (ComboOrientation.SelectedItem is ComboBoxItem orientItem)
                ticket.PageOrientation = orientItem.Content.ToString() == "Landscape"
                                       ? PageOrientation.Landscape
                                       : PageOrientation.Portrait;

            // Sync paper size [1]
            if (ComboPaperSize.SelectedItem is ComboBoxItem sizeItem)
                switch (sizeItem.Content.ToString())
                {
                    case "A4":
                        ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
                        break;

                    case "Letter":
                        ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetter);
                        break;

                    case "A5":
                        ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA5);
                        break;
                }

            // Assign the updated ticket to the print dialog before it opens [1]
            printDlg.PrintTicket = ticket;

            // 2. Vis dialogen (Nu vil den have valgt det rigtige format derinde!) [2]
            if (printDlg.ShowDialog() == true)
            {
                // 3. Hent dokumentet og udskriv
                if (MyDocumentViewer.Document is FixedDocumentSequence currentDocument)
                    printDlg.PrintDocument(currentDocument.DocumentPaginator, "Mit Dynamiske Dokument");

                this.Close();
            }
        }
        catch (Exception ex)
        {
            // Some virtual printer drivers may fail validation - handle it gracefully
            System.Diagnostics.Debug.WriteLine($"Could not configure PrintTicket: {ex.Message}");
        }

        e.Handled = true;
    }

    private void Print_Click2(object sender, RoutedEventArgs e)
    {
        var printDlg = new System.Windows.Controls.PrintDialog();

        if (printDlg.ShowDialog() == true)
        {
            if (MyDocumentViewer.Document is FixedDocumentSequence currentDocument)
                printDlg.PrintDocument(currentDocument.DocumentPaginator, "Mit Dynamiske Dokument");
        }
    }

    private void OnPageSetupChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboPaperSize == null || MyDocumentViewer == null)
            return;

        UpdatePreview();
    }

    private void UpdatePreview()
    {
        // 1. Calculate size from UI choices (96 units = 1 inch)
        double    width  = 8.27 * 96;  // Standard A4
        double    height = 11.69 * 96;
        Thickness margin = new Thickness(40); // Can also be made dynamic if desired

        if (ComboPaperSize.SelectedItem is ComboBoxItem sizeItem)
            switch (sizeItem.Content.ToString())
            {
                case "Letter":
                    width  = 8.5 * 96;
                    height = 11.0 * 96;
                    break;

                case "A5":
                    width  = 5.83 * 96;
                    height = 8.27 * 96;
                    break;
            }

        if (ComboOrientation.SelectedItem is ComboBoxItem orientItem && 
            orientItem.Content.ToString() == "Landscape")
        {
            double temp = width;
            width       = height;
            height      = temp;
        }

        Size targetSize = new Size(width, height);

        // 2. Regenerate entire FixedDocumentSequence with new dimensions
        FixedDocumentSequence updatedSequence = CreateFixedDocumentSequence( _headerEl
                                                                           , _centralEl
                                                                           , _footerEl
                                                                           , _contentHeaderHeight
                                                                           , targetSize
                                                                           , margin);

        // 3. Push the new document into the viewer - it updates immediately on screen
        MyDocumentViewer.Document = updatedSequence;
    }

    private static FixedDocumentSequence CreateFixedDocumentSequence( FrameworkElement headerElement
                                                                    , FrameworkElement centralElement
                                                                    , FrameworkElement footerElement
                                                                    , double contentHeaderHeight
                                                                    , Size pageSize       // Receive size dynamically
                                                                    , Thickness margin)    // Receive margins dynamically
    {
        // 1. Clone and render elements based on the NEW available width
        var headerClone  = PrepareClone(headerElement);
        var contentClone = PrepareClone(centralElement);
        var footerClone  = PrepareClone(footerElement);

        // Tip: Make sure your PrepareClone/RenderElement methods measure (Measure/Arrange)
        // elements according to the new 'pageSize.Width - margin.Left - margin.Right', 
        // so the content actually stretches or shrinks to the new paper size.
        var headerBmp  = RenderElement(headerClone);
        var contentBmp = RenderElement(contentClone);
        var footerBmp  = RenderElement(footerClone);

        double headerHeight = FindHeaderHeight(centralElement, contentHeaderHeight);

        // 2. Create your paginator with the new dynamic dimensions
        var paginator = new BitmapPaginator(
                                             headerBmp
                                           , contentBmp
                                           , footerBmp
                                           , pageSize
                                           , margin
                                           , headerHeight);

        var fixedDoc = new FixedDocument();

        for (int i = 0; i <  paginator.PageCount; i++)
        {
            var page = paginator.GetPage(i);

            var pc        = new PageContent();
            var fixedPage = new FixedPage
                            {
                                Width  = pageSize.Width
                              , Height = pageSize.Height
                            };

            fixedPage.Children.Add((UIElement)page.Visual);

            ((IAddChild)pc).AddChild(fixedPage);
            fixedDoc.Pages.Add(pc);
        }

        var fds    = new FixedDocumentSequence();
        var docRef = new DocumentReference();
        docRef.SetDocument(fixedDoc);
        fds.References.Add(docRef);

        return fds;
    }

    // ------------------------------------------------------------
    // CLONE + PREPARE
    // ------------------------------------------------------------
    private static FrameworkElement PrepareClone(FrameworkElement element)
    {
        if (element == null)
            return null;

        var clone = CloneHelper.CloneElement(element);

        RemoveScrollViewer(clone);
        DisableSfDataGridVirtualization(clone);

        return clone;
    }

    private static void RemoveScrollViewer(FrameworkElement element)
    {
        foreach (var sv in FindVisualChildren<ScrollViewer>(element))
        {
            sv.VerticalScrollBarVisibility   = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.CanContentScroll              = false;
        }
    }

    private static void DisableSfDataGridVirtualization(FrameworkElement element)
    {
        foreach (var dg in FindVisualChildren<Syncfusion.UI.Xaml.Grid.SfDataGrid>(element))
        {
            dg.EnableDataVirtualization = false;
            dg.GridColumnSizer          = null;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent == null)
            yield break;

        for (int i = 0; i <  VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T t)
                yield return t;

            foreach (var sub in FindVisualChildren<T>(child))
                yield return sub;
        }
    }

    // ------------------------------------------------------------
    // RENDER CLONE → BITMAP
    // ------------------------------------------------------------
    private static RenderTargetBitmap RenderElement(FrameworkElement element)
    {
        if (element == null)
            return null;

        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        element.Arrange(new Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
        element.UpdateLayout();

        int width  = (int)element.DesiredSize.Width;
        int height = (int)element.DesiredSize.Height;

        var bmp = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(element);

        return bmp;
    }

    private static double FindHeaderHeight(FrameworkElement element, double defaultHeight)
    {
        foreach (var dg in FindVisualChildren<DataGrid>(element))
            return dg.ColumnHeaderHeight + 80;

        return defaultHeight;
    }
}
