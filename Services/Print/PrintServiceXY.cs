using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using global::DBF.Helpers;
using PrintDialogXY;
using Syncfusion.UI.Xaml.Grid;
using PrintDialog = PrintDialogXY.PrintDialog;
using PrintDocument = PrintDialogXY.PrintDocument;

namespace DBF.Services;

public static class PrintServiceXY
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
        public static void Print( FrameworkElement header
                                , FrameworkElement content
                                , FrameworkElement footer
                                , string title = null
                                , double contentHeaderHeight = 120)
        {
            // 1. Prepare clones 
            var headerClone  = PrepareClone(header);
            var contentClone = PrepareClone(content);
            var footerClone  = PrepareClone(footer);

            // 2. Render to bitmaps
            var headerBmp  = RenderElement(headerClone);
            var contentBmp = RenderElement(contentClone);
            var footerBmp  = RenderElement(footerClone);

            // 3. Find header height (if content contains a DataGrid)
            double headerHeight = FindHeaderHeight(contentClone, contentHeaderHeight);

            // initial settings (fx A4 portrait)
            var settings = new PrintSettings
                           {
                               Orientation = PageOrientation.Portrait
                             , PageSize    = new Size(793, 1122)
                             , Margin      = new Thickness(40)
                           };

            var dlg = new PrintDialog();

            // Create the initial document 
            dlg.Document = new();

            // Is't here the magic happens
            dlg.Document.PrintSettingsChanged+= (s, e) =>
            {
                if (s is not PrintDialogXY.PrintDocument document)
                    return;

                // Delay the preview generation until the document is updated.
                e.IsUpdating = null;

                // Convert Layout to PageOrientationY
                var orientation = e.CurrentSettings.Layout == PrintDialogXY.Enums.Layout.Portrait
                                ? PageOrientation.Portrait
                                : PageOrientation.Landscape;

                // Default PageSize, Margin and Settings
                var pageSize = new Size( (double)(e.CurrentSettings.Size?.Width  ?? 793)
                                       , (double)(e.CurrentSettings.Size?.Height ?? 1122)
                                       );

                var margin = e.CurrentSettings.Margin switch
                {
                    PrintDialogXY.Enums.Margin.Default => new Thickness(20),
                    PrintDialogXY.Enums.Margin.None    => new Thickness(0),
                    PrintDialogXY.Enums.Margin.Minimum => new Thickness(10),
                    PrintDialogXY.Enums.Margin.Custom  => new Thickness(30),
                    _                                 => new Thickness(0)
                };

                var newSettings = new PrintSettings
                                  {
                                      Orientation = orientation
                                    , PageSize    = pageSize
                                    , Margin      = margin
                                  };

                // Go build the document
                BuildDocument( headerBmp
                             , contentBmp
                             , footerBmp
                             , headerHeight
                             , newSettings
                             , title
                             , document);
            };

            dlg.ShowDialog();
        }

        private static PrintDocument BuildDocument( RenderTargetBitmap headerBmp
                                                  , RenderTargetBitmap contentBmp
                                                  , RenderTargetBitmap footerBmp
                                                  , double headerHeight
                                                  , PrintSettings settings
                                                  , string title
                                                  , PrintDocument document = null)
        {
            // 1. pageSize depends on orientation
            Size pageSize = settings.Orientation == PageOrientation.Landscape
                          ? new Size(settings.PageSize.Height, settings.PageSize.Width)
                          : settings.PageSize;

            // 2. set paginator
            var paginator = new BitmapPaginator( headerBmp
                                               , contentBmp
                                               , footerBmp
                                               , pageSize
                                               , settings.Margin
                                               , headerHeight);

            // 3. Build PrintDocument
            var doc = document ?? new PrintDocument();

            doc.Pages.Clear();

            for (int i = 0; i <  paginator.PageCount; i++)
            {
                var page = paginator.GetPage(i);
          

                doc.Pages.Add(new PrintPage
                              {
                                  Content = (FrameworkElement)page.Visual
                              });
            }

            //for (int i = 0; i <  paginator.PageCount; i++)
            //{
            //    var page = paginator.GetPage(i);

            //    var pc        = new PageContent();
            //    var fixedPage = new FixedPage
            //                    {
            //                        Width  = paginator.PageSize.Width
            //                      , Height = paginator.PageSize.Height
            //                    };

            //    fixedPage.Children.Add((UIElement)page.Visual);

            //    ((IAddChild)pc).AddChild(fixedPage);
            //    doc.Pages.Add(pc);
            //}

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

