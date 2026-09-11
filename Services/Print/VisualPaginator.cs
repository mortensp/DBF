using System.Windows;
using System.Windows.Documents;
using Syncfusion.UI.Xaml.Grid;
using Point = System.Windows.Point;


namespace DBF.Services;

public class VisualPaginator : DocumentPaginator
{
    private readonly FrameworkElement _element;
    private readonly PrintSettings _settings;

    private readonly Size _pageSize;
    private readonly double _scale;
    private readonly Thickness _margin;

    private readonly double _usableHeight;

    public VisualPaginator(FrameworkElement element, PrintSettings settings)
    {
        _element = element;
        _settings = settings;

        _pageSize = settings.PageSize;
        _scale = settings.Scale;
        _margin = settings.Margin;

        _element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _element.Arrange(new Rect(_element.DesiredSize));
        _element.UpdateLayout();

        _usableHeight = (_pageSize.Height - _margin.Top - _margin.Bottom) / _scale;
    }

    public override bool IsPageCountValid => true;

    public override int PageCount =>
        (int)Math.Ceiling(_element.ActualHeight / _usableHeight);

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException();
    }

    public override IDocumentPaginatorSource Source => null;

    public override DocumentPage GetPage(int pageNumber)
    {
        double yOffset = pageNumber * _usableHeight;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(_scale, _scale));

            dc.DrawRectangle(Brushes.White, null, new Rect(new Point(0, 0), _pageSize));

            dc.PushClip(new RectangleGeometry(new Rect(
                _margin.Left / _scale,
                _margin.Top / _scale,
                _pageSize.Width / _scale - _margin.Left - _margin.Right,
                _usableHeight)));

            dc.PushTransform(new TranslateTransform(
                _margin.Left / _scale,
                _margin.Top / _scale - yOffset));

            dc.DrawRectangle(new VisualBrush(_element), null,
                new Rect(new Point(0, 0), new Size(_element.ActualWidth, _element.ActualHeight)));

            dc.Pop();
            dc.Pop();
        }

        return new DocumentPage(dv, _pageSize, new Rect(_pageSize), new Rect(_pageSize));
    }
}
