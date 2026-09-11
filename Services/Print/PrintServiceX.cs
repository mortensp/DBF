using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using global::DBF.Helpers;
using PrintDialogX;
using Syncfusion.UI.Xaml.Grid;
using PrintDialog = PrintDialogX.PrintDialog;
using PrintDocument = PrintDialogX.PrintDocument;

namespace DBF.Services;

public static class PrintServiceX
{
    #region  PUBLIC API
    /// <summary>
    /// Prints the specified header, content, and footer using a PrintDialog.
    /// </summary>
    /// <remarks>Displays a PrintDialog and prints the generated FixedDocumentSequence; no printing occurs if
    /// the user cancels the dialog.</remarks>
    /// <param name="content">Main content element to print.</param>
    /// <param name="title">Optional print job title; "Print" is used if null.</param>
    /// <param name="contentHeaderHeight">Height, in device-independent units (1/96 inch), reserved for the content header area.</param>
    public static void Print(FrameworkElement content, string title = null, double contentHeaderHeight = 120)

    {
        Print(null, content, null, title, contentHeaderHeight);
    }

    /// <summary>
    /// Prints the specified header, content, and footer using a PrintDialog.
    /// </summary>
    /// <remarks>Displays a PrintDialog and prints the generated FixedDocumentSequence; no printing occurs if
    /// the user cancels the dialog.</remarks>
    /// <param name="header">Element displayed at the top of each printed page.</param>
    /// <param name="content">Main content element to print.</param>
    /// <param name="title">Optional print job title; "Print" is used if null.</param>
    /// <param name="contentHeaderHeight">Height, in device-independent units (1/96 inch), reserved for the content header area.</param>
    public static void Print(FrameworkElement header, FrameworkElement content, string title = null, double contentHeaderHeight = 120)

    {
        Print(header, content, null, title, contentHeaderHeight);
    }

    /// <summary>
    /// Prints the specified header, content, and footer using a PrintDialog.
    /// </summary>
    /// <remarks>Displays a PrintDialog and prints the generated FixedDocumentSequence; no printing occurs if
    /// the user cancels the dialog.</remarks>
    /// <param name="header">Element displayed at the top of each printed page.</param>
    /// <param name="content">Main content element to print.</param>
    /// <param name="footer">Element displayed at the bottom of each printed page.</param>
    /// <param name="title">Optional print job title; "Print" is used if null.</param>
    /// <param name="contentHeaderHeight">Height, in device-independent units (1/96 inch), reserved for the content header area.</param>
    public static void Print(FrameworkElement header
                            , FrameworkElement content
                            , FrameworkElement footer
                            , string title = null
                            , double contentHeaderHeight = 120)
    {
        var headerClone  = PrepareClone(header);
        var contentClone = PrepareClone(content);
        var footerClone  = PrepareClone(footer);

        // initial settings (fx A4 portrait)
        var settings = new PrintSettings
        {
            Orientation = PageOrientation.Portrait
                             ,
            PageSize    = new Size(793, 1122)
                             ,
            Margin      = new Thickness(40)
        };

        var dlg = new PrintDialog();

        // første dokument
        dlg.Document = new();
        // BuildDocument(headerClone
                                    //, contentClone
                                    //, footerClone
                                    //, contentHeaderHeight
                                    //, settings
                                    //, title);

        // når brugeren ændrer orientation / page size / margins:
        dlg.Document.PrintSettingsChanged += (s, e) =>
        {
            if (s is not PrintDialogX.PrintDocument document)
                return;

            // Delay the preview generation until the document is updated.
            e.IsUpdating = null;

            var newSettings = new PrintSettings
            {
                Orientation = e.CurrentSettings.Layout == PrintDialogX.Enums.Layout.Portrait
                                                  ? PageOrientation.Landscape
                                                  : PageOrientation.Portrait
               ,
                PageSize    = new( (double)(e.CurrentSettings.Size?.Width  ?? 793)
                                 , (double)(e.CurrentSettings.Size?.Height ?? 1122)
                                 )
               ,
                Margin      = e.CurrentSettings.Margin switch
                {
                    PrintDialogX.Enums.Margin.Default => new Thickness(20),
                    PrintDialogX.Enums.Margin.None    => new Thickness(0),
                    PrintDialogX.Enums.Margin.Minimum => new Thickness(10),
                    PrintDialogX.Enums.Margin.Custom  => new Thickness(30),
                    _                                 => new Thickness(0)
                }
            };

            BuildDocument(headerClone
                                        , contentClone
                                        , footerClone
                                        , contentHeaderHeight
                                        , newSettings
                                        , title
                                        , document);
            // Signal the preview generation to update.
            //e.IsUpdating = true;

        };

        dlg.ShowDialog();
    }

    private static PrintDocument BuildDocument(FrameworkElement headerClone
                                              , FrameworkElement contentClone
                                              , FrameworkElement footerClone
                                              , double contentHeaderHeight
                                              , PrintSettings settings
                                              , string title
                                              , PrintDocument document = null)
    {
        // 1. Render til bitmaps
        var headerBmp  = RenderElement(headerClone);
        var contentBmp = RenderElement(contentClone);
        var footerBmp  = RenderElement(footerClone);

        // 2. pageSize afhænger af orientation
        Size pageSize = settings.Orientation == PageOrientation.Landscape
                          ? new Size(settings.PageSize.Height, settings.PageSize.Width)
                          : settings.PageSize;

        double headerHeight = FindHeaderHeight(contentClone, contentHeaderHeight);

        var paginator = new BitmapPaginator(
                                                 headerBmp
                                               , contentBmp
                                               , footerBmp
                                               , pageSize
                                               , settings.Margin
                                               , headerHeight);

        // 3. byg PrintDocument
        var doc = document ?? new PrintDocument
        {
            //Title = title ?? "Print"
        };

        doc.Pages.Clear();

        for (int i = 0; i < paginator.PageCount; i++)
        {
            var page = paginator.GetPage(i);

            doc.Pages.Add(new PrintPage
            {
                Content = (FrameworkElement)page.Visual
                //, PageSize    = pageSize
                //, Orientation = settings.Orientation
            });
        }

        return doc;
    }
    #endregion

    #region private Methods
    // ------------------------------------------------------------
    // CLONE + PREPARE (genbruger din eksisterende kode)
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
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.CanContentScroll = false;
        }
    }

    private static void DisableSfDataGridVirtualization(FrameworkElement element)
    {
        foreach (var dg in FindVisualChildren<SfDataGrid>(element))
        {
            dg.EnableDataVirtualization = false;
            dg.GridColumnSizer = null;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent == null)
            yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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
    #endregion
}

