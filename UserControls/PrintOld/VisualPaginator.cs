using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace DBF.Services.Print;

public class VisualPaginator : DocumentPaginator
{
    private readonly FrameworkElement _visual;
    private readonly Size             _pageSize;
    private readonly Thickness        _margin;

    public VisualPaginator(FrameworkElement visual, Size pageSize, Thickness margin)
    {
        _visual   = visual;
        _pageSize = pageSize;
        _margin   = margin;

        _visual.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        _visual.Arrange(new Rect(_visual.DesiredSize));
    }

    public override int    PageCount     =>
        (int)Math.Ceiling((_visual.DesiredSize.Height - HeaderHeight) /
                                          (ContentHeight));

    private double HeaderHeight { get; set; } = 120;     // Header + DataGrid kolonne header
    private         double ContentHeight => _pageSize.Height - _margin.Top - _margin.Bottom - HeaderHeight;

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException();
    }

    public override IDocumentPaginatorSource Source           => null;

    public override bool                     IsPageCountValid => true;

    public override DocumentPage GetPage(int pageNumber)
    {
        var canvas = new Canvas
                     {
                         Width  = _pageSize.Width
                       , Height = _pageSize.Height
                     };

        // Clone header (top part of UserControl)
        var header = CloneElement(_visual, new Rect(0, 0, _pageSize.Width, HeaderHeight));
        Canvas.SetTop(header, _margin.Top);
        canvas.Children.Add(header);

        // Clone content (only the part belonging to this page)
        double yOffset = HeaderHeight + pageNumber * ContentHeight;
        var    content = CloneElement(_visual, new Rect(0, yOffset, _pageSize.Width, ContentHeight));
        Canvas.SetTop(content, HeaderHeight + _margin.Top);
        canvas.Children.Add(content);

        return new DocumentPage(canvas);
    }

    private FrameworkElement CloneElement(FrameworkElement original, Rect region)
    {
        var brush = new VisualBrush(original)
                    {
                        Stretch      = Stretch.None
                      , AlignmentX   = AlignmentX.Left
                      , AlignmentY   = AlignmentY.Top
                      , ViewboxUnits = BrushMappingMode.Absolute
                      , Viewbox      = region
                    };

        return new Border
               {
                   Width      = region.Width
                 , Height     = region.Height
                 , Background = brush
               };
    }
}
