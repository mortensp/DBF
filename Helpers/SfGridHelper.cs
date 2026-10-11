using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;

namespace DBF.Helpers;

/// <summary>
/// Helper class for Syncfusion SfDataGrid to support auto-sizing of rows and columns based on content. 
/// </summary>    
public static class SfGridHelper
{
    public static void QueryRowHeight(object sender, QueryRowHeightEventArgs e)
    {
        var dataGrid = sender as SfDataGrid;

        if (dataGrid == null)
            return;

        // Fetch visual row
        var recordEntry = dataGrid.GetNodeEntry(e.RowIndex);

        if (recordEntry is null
        ||  recordEntry.IsGroups)
            return;

        var record = dataGrid.GetRecordAtRowIndex(e.RowIndex);

        if (record == null)
            return;

        double maxHeight = dataGrid.RowHeight;

        // Get device DPI -> pixelsPerDip
        var    dpi          = VisualTreeHelper.GetDpi(dataGrid);
        double pixelsPerDip = dpi.PixelsPerDip;

        foreach (var col in dataGrid.Columns.OfType<GridTextColumn>().Where(c => c.TextWrapping == TextWrapping.Wrap))
        {
            var value = record.GetType().GetProperty(col.MappingName)?.GetValue(record)?.ToString();

            if (string.IsNullOrEmpty(value))
                continue;

            var fontFamily = dataGrid.FontFamily;
            var fontSize   = dataGrid.FontSize;
            var typeface   = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            // Use overload that accepts pixelsPerDip (DPI aware measurement)
            var formattedText = new FormattedText( value
                                                 , CultureInfo.CurrentCulture
                                                 , FlowDirection.LeftToRight
                                                 , typeface
                                                 , fontSize
                                                 , Brushes.Black
                                                 , new NumberSubstitution()
                                                 , TextFormattingMode.Display
                                                 , pixelsPerDip);

            // Consider column padding when measuring. Use horizontal padding to compute available
            // text width, and vertical padding when adding to the required row height.
            var horizontalPadding = col.Padding.Left + col.Padding.Right;
            var verticalPadding   = col.Padding.Top + col.Padding.Bottom + 4; // small extra gap

            // If this control is placed inside a Viewbox it will be scaled. In that case we must
            // measure the text as it will appear visually (scaled) and then convert the measured
            // visible height back to the unscaled row height that SfDataGrid expects.
            double scaleX = 1.0, scaleY = 1.0;
            try
            {
                var wnd = Window.GetWindow(dataGrid);

                if (wnd is not null)
                {
                    var gt = dataGrid.TransformToVisual(wnd);
                    var p0 = gt.Transform(new Point(0, 0));
                    var pX = gt.Transform(new Point(1, 0));
                    var pY = gt.Transform(new Point(0, 1));
                    scaleX = Math.Abs(pX.X - p0.X);
                    scaleY = Math.Abs(pY.Y - p0.Y);

                    if (scaleX <= 0)
                        scaleX =  1.0;

                    if (scaleY <= 0)
                        scaleY =  1.0;
                }
            }

            catch
            {
                // fall back to 1.0 if transform not available
                scaleX = scaleY = 1.0;
            }

            var availableTextWidth = Math.Max(1.0, (col.ActualWidth - horizontalPadding) * scaleX);

            // Measure text at the scaled font size so FormattedText.Height represents visible height
            var scaledTypeface = typeface;
            var scaledFontSize = fontSize * scaleY;

            var visFormatted = new FormattedText(formattedText.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, scaledTypeface, scaledFontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip)
                               {
                                   MaxTextWidth = availableTextWidth
                               };

            // convert visible height back to unscaled height used by SfDataGrid
            var requiredUnscaledHeight = (visFormatted.Height / scaleY);

            if (requiredUnscaledHeight + verticalPadding >  maxHeight)
                maxHeight = Math.Max(maxHeight, requiredUnscaledHeight + verticalPadding);
        }

        e.Height  = maxHeight;
        e.Handled = true;
    }

    public static void AutoSizeTextColumns(SfDataGrid dg)
    {
        var    dpi          = VisualTreeHelper.GetDpi(dg);
        double pixelsPerDip = dpi.PixelsPerDip;
        var    typeface     = new Typeface(dg.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var    items        = ((ListCollectionView)dg.ItemsSource).SourceCollection;

        foreach (var col in dg.Columns.OfType<GridTextColumn>().Where(c => c.TextWrapping != TextWrapping.Wrap))
        {
            if (col.ColumnSizer is  not GridLengthUnitType.None
                                and not GridLengthUnitType.SizeToCells)
                    continue;


            // header
            var header   = (col.HeaderText ?? string.Empty);
            var ftHeader = new FormattedText(header, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, dg.FontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip);
            var max          = ftHeader.Width;

            if (col.AllowSorting)
                max += 20;

            // cells
            foreach (var item in items)
            {
                var v = item?.GetType().GetProperty(col.MappingName)?.GetValue(item)?.ToString();

                if (string.IsNullOrEmpty(v))
                    continue;

                var ft = new FormattedText(v, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, dg.FontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip);
                max    = Math.Max(max, ft.Width);
            }

            col.Width = max + col.Padding.Left + col.Padding.Right + 8;
        }

        dg.UpdateLayout();
    }
}
