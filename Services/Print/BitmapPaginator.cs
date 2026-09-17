using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace DBF.Helpers;

public class BitmapPaginator : DocumentPaginator
{
    private readonly RenderTargetBitmap _headerBmp;
    private readonly RenderTargetBitmap _contentBmp;
    private readonly RenderTargetBitmap _footerBmp;

    private readonly Size      _pageSize;
    private readonly Thickness _margin;

    private readonly double _headerHeight;
    private readonly double _footerHeight;
    private readonly double _contentHeaderHeight;

    private readonly double _pageContentHeight;

    private readonly double _scale;

    public BitmapPaginator( RenderTargetBitmap headerBmp
                          , RenderTargetBitmap contentBmp
                          , RenderTargetBitmap footerBmp
                          , Size pageSize
                          , Thickness margin
                          , double contentHeaderHeight)
    {
        _headerBmp  = headerBmp;
        _contentBmp = contentBmp;
        _footerBmp  = footerBmp;

        _pageSize = pageSize;
        _margin   = margin;

        _headerHeight        = headerBmp?.PixelHeight ?? 0;
        _footerHeight        = footerBmp?.PixelHeight ?? 0;
        _contentHeaderHeight = contentHeaderHeight;

        // Skalering: udnyt hele sidebredden
        //_scale = (_pageSize.Width-margin.Left- margin.Right) / _contentBmp.PixelWidth;
        _scale = 1;

        _pageContentHeight = _pageSize.Height
                           - _margin.Top
                           - (_headerHeight * _scale)
                           - (_contentHeaderHeight * _scale)
                           - (_footerHeight * _scale)
                           - _margin.Bottom;
    }

    public override bool                     IsPageCountValid => true;

    public override int                      PageCount        => (int)(Math.Ceiling((_contentBmp.PixelHeight - _contentHeaderHeight)
                                                               * _scale / _pageContentHeight));

    public override IDocumentPaginatorSource Source           => null;

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException();
    }

    public override DocumentPage GetPage(int pageNumber)
    {
        double yOffsetBmp = pageNumber * _pageContentHeight / _scale + _contentHeaderHeight;

        double yOffsetPage = _margin.Top;

        var canvas = new Canvas
                     {
                         Width  = _pageSize.Width
                       , Height = _pageSize.Height
                     };

        // HEADER
        if (_headerBmp != null && _headerHeight >  0)
            AddToCanvas( _headerHeight * _scale
                       , CreateImageRegion( _headerBmp
                                          , new Rect(0, 0, _headerBmp.PixelWidth, _headerHeight)));

        // CONTENT HEADER
        if (_contentHeaderHeight >  0)
            AddToCanvas( _contentHeaderHeight * _scale
                       , CreateImageRegion( _contentBmp
                                          , new Rect(0, 0, _contentBmp.PixelWidth, _contentHeaderHeight)));

        // CONTENT PAGE
        AddToCanvas( _pageContentHeight
                   , CreateImageRegion( _contentBmp
                                      , new Rect(0, yOffsetBmp, _contentBmp.PixelWidth, _pageContentHeight / _scale)));

        // FOOTER
        if (_footerBmp != null && _footerHeight >  0)
            AddToCanvas( _footerHeight * _scale
                       , CreateImageRegion( _footerBmp
                                          , new Rect(0, 0, _footerBmp.PixelWidth, _footerHeight)));

        canvas.Measure(_pageSize);
        canvas.Arrange(new Rect(new System.Windows.Point(), _pageSize));
        canvas.UpdateLayout();

        return new DocumentPage(canvas);

        void AddToCanvas(double height, Image image)
        {
            Canvas.SetLeft(image, _margin.Left);
            Canvas.SetTop(image, yOffsetPage);

            canvas.Children.Add(image);

            yOffsetPage+= height;
        }
    }

    private Image CreateImageRegion(RenderTargetBitmap bmp, Rect region)
    {
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            dc.DrawImage(
                          bmp
                        , new Rect(
                                    -region.X * _scale
                                  , -region.Y * _scale
                                  , bmp.PixelWidth * _scale
                                  , bmp.PixelHeight * _scale));
        }

        var croppedBmp = new RenderTargetBitmap(
                                                 (int)(region.Width * _scale)
                                               , (int)(region.Height * _scale)
                                               , 96, 96
                                               , PixelFormats.Pbgra32);

        croppedBmp.Render(dv);

        return new Image
               {
                   Source = croppedBmp
                 , Width  = region.Width * _scale
                 , Height = region.Height * _scale
               };
    }
}
