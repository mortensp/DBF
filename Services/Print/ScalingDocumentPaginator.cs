using System.Windows;
using System.Windows.Documents;

namespace DBF.Services.Print;

public sealed class ScalingDocumentPaginator : DocumentPaginator
{
    private readonly DocumentPaginator _source;
    private readonly Size _targetPageSize;

    public ScalingDocumentPaginator(
        DocumentPaginator source,
        Size targetPageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        _source = source;
        _targetPageSize = targetPageSize;

        _source.PageSize = targetPageSize;
    }

    public override bool IsPageCountValid =>
        _source.IsPageCountValid;

    public override int PageCount =>
        _source.PageCount;

    public override Size PageSize
    {
        get => _targetPageSize;
        set => throw new NotSupportedException();
    }

    public override IDocumentPaginatorSource Source =>
        _source.Source;

    public override DocumentPage GetPage(int pageNumber)
    {
        DocumentPage sourcePage = _source.GetPage(pageNumber);

        if (sourcePage == DocumentPage.Missing)
            return sourcePage;

        Size sourceSize = sourcePage.Size;

        double scaleX =
            _targetPageSize.Width / sourceSize.Width;

        double scaleY =
            _targetPageSize.Height / sourceSize.Height;

        // Keep aspect ratio.
        double scale = Math.Min(scaleX, scaleY);

        double offsetX =
            (_targetPageSize.Width -
             sourceSize.Width * scale) / 2;

        double offsetY =
            (_targetPageSize.Height -
             sourceSize.Height * scale) / 2;

        var visual = new DrawingVisual();

        using (DrawingContext dc =
               visual.RenderOpen())
        {
            dc.PushTransform(
                new TranslateTransform(offsetX, offsetY));

            dc.PushTransform(
                new ScaleTransform(scale, scale));

            dc.DrawRectangle(
                Brushes.White,
                null,
                new Rect(
                    0,
                    0,
                    sourceSize.Width,
                    sourceSize.Height));

            dc.DrawRectangle(
                new VisualBrush(sourcePage.Visual),
                null,
                new Rect(
                    0,
                    0,
                    sourceSize.Width,
                    sourceSize.Height));

            dc.Pop();
            dc.Pop();
        }

        return new DocumentPage(
            visual,
            _targetPageSize,
            new Rect(
                offsetX,
                offsetY,
                sourceSize.Width * scale,
                sourceSize.Height * scale),
            new Rect(
                offsetX,
                offsetY,
                sourceSize.Width * scale,
                sourceSize.Height * scale));
    }
}
