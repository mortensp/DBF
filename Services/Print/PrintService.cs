using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using DBF.Helpers;
using DBF.Services.Print;
using Syncfusion.UI.Xaml.Grid;
using PrintDialog = System.Windows.Controls.PrintDialog;

namespace DBF.Services;

public static class PrintService
{
    private static string _previewWindowXaml = 
                          @"<Window
            xmlns='http://schemas.microsoft.com/netfx/2007/xaml/presentation'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            Title='Print Preview - @@TITLE'
            Height='800'
            Width='600'
            WindowStartupLocation='CenterOwner'>
            <DocumentViewer Name='dv1'/>
        </Window>";

    #region  PUBLIC API
        #region Preview
            /// <summary>
            ///  Preview a FrameworkElement in a print preview window. The element will be cloned and rendered to a bitmap for accurate representation.
            /// </summary>
            /// <param name="content">The FrameworkElement to preview</param>
            /// <param name="title">The title of the print preview window</param>
            /// <param name="contentHeaderHeight">The height of the content header</param>
            public static void Preview(FrameworkElement content, string title = null, double contentHeaderHeight = 120)

            {
                Preview(null, content, null, title, contentHeaderHeight);
            }

            /// <summary>
            ///  Preview a FrameworkElement in a print preview window. The element will be cloned and rendered to a bitmap for accurate representation.
            /// </summary>
            /// <param name="header">The header element</param>
            /// <param name="content">The FrameworkElement to preview</param>
            /// <param name="title">The title of the print preview window</param>
            /// <param name="contentHeaderHeight">The height of the content header</param>
            public static void Preview(FrameworkElement header, FrameworkElement content, string title = null, double contentHeaderHeight = 120)

            {
                Preview(header, content, null, title, contentHeaderHeight);
            }

            /// <summary>
            ///  Preview a FrameworkElement in a print preview window. The element will be cloned and rendered to a bitmap for accurate representation.
            /// </summary>
            /// <param name="header">The header element</param>
            /// <param name="content">The FrameworkElement to preview</param>
            /// <param name="footer">The footer element</param>
            /// <param name="title">The title of the print preview window</param>
            /// <param name="contentHeaderHeight">The height of the content header</param>
            public static void Preview( FrameworkElement header
                                      , FrameworkElement content
                                      , FrameworkElement footer
                                      , string title = null
                                      , double contentHeaderHeight = 120)
            {
                //Report: En meget simpel Bitmap løsning, der virker
                var fds = CreateFixedDocumentSequence(header, content, footer, contentHeaderHeight);

#if true
                string s = _previewWindowXaml.Replace("@@TITLE", title ?? "");

                using var    reader = new System.Xml.XmlTextReader(new StringReader(s));
                Window       window = System.Windows.Markup.XamlReader.Load(reader) as Window;

                DocumentViewer dv1 = LogicalTreeHelper.FindLogicalNode(window, "dv1") as DocumentViewer;
                dv1.Document       = fds;
#else
                var window = new Window
                             {
                                 Title                 = "Print Preview"
                               , Width                 = 800
                               , Height                = 600
                               , WindowStartupLocation = WindowStartupLocation.CenterOwner
                             };

                //var doc = new PaginatorDocument(paginator);
                var dv = new DocumentViewer
                         {
                             Document = fds
                         };

                window.Content = dv;
#endif
                window.Owner = Application.Current.MainWindow;
                window.ShowDialog();
            }
        #endregion

        /// <summary>
        /// Prints the specified header, content, and footer using a PrintDialog.
        /// </summary>
        /// <remarks>Displays a PrintDialog and prints the generated FixedDocumentSequence; no printing occurs if
        /// the user cancels the dialog.</remarks>
        /// <param name="content">Main content element to print.</param>
        /// <param name="title">Optional print job title; "Print" is used if null.</param>
        /// <param name="contentHeaderHeight">Height, in device-independent units (1/96 inch), reserved for the content header area.</param>
        #region Print
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
            public static void Print( FrameworkElement header
                                    , FrameworkElement content
                                    , FrameworkElement footer
                                    , string title = null
                                    , double contentHeaderHeight = 120)
            {
                var dlg = new PrintDialog();

                if (dlg.ShowDialog() != true)
                    return;

                var fds = CreateFixedDocumentSequence(header, content, footer, contentHeaderHeight);

                //dlg.PrintDocument(fds.DocumentPaginator, title ?? "Print");
                var preview = new PrintPreviewWindow( fds
                                                    , fds.DocumentPaginator);

                preview.ShowDialog();
            }
        #endregion
    #endregion

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
        foreach (var dg in FindVisualChildren<SfDataGrid>(element))
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
    // FIXEDDOCUMENT (NO XPS)
    // ------------------------------------------------------------
    private static FixedDocumentSequence CreateFixedDocumentSequence( FrameworkElement headerElement
                                                                    , FrameworkElement centralElement
                                                                    , FrameworkElement footerElement
                                                                    , double contentHeaderHeight)
    {
        var headerClone  = PrepareClone(headerElement);
        var contentClone = PrepareClone(centralElement);
        var footerClone  = PrepareClone(footerElement);

        var headerBmp  = RenderElement(headerClone);
        var contentBmp = RenderElement(contentClone);
        var footerBmp  = RenderElement(footerClone);

        Size      pageSize = new Size(793, 1122);
        Thickness margin   = new Thickness(40);

        double headerHeight = FindHeaderHeight(centralElement, contentHeaderHeight);

        var paginator = new BitmapPaginator( headerBmp
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
                                Width  = paginator.PageSize.Width
                              , Height = paginator.PageSize.Height
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
