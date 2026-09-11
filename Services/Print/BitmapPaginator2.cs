using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Image = System.Windows.Controls.Image;

namespace DBF.Helpers;

public class BitmapPaginator2 : DocumentPaginator
{
    private readonly RenderTargetBitmap _headerBmp;
    private readonly RenderTargetBitmap _contentBmp;
    private readonly RenderTargetBitmap _footerBmp;

    private readonly Size      _pageSize;
    private readonly Thickness _margin;

    private readonly double _headerHeight;
    private readonly double _footerHeight;
    private readonly double _pageContentHeight;
    private readonly double _contentHeaderHeight;

    public BitmapPaginator2( RenderTargetBitmap contentBmp
                          , Size pageSize
                          , Thickness? margin = null
                          , double contentHeaderHeight = 0)
    : this(null, contentBmp, null, pageSize, margin, contentHeaderHeight)
    {
    }

    public BitmapPaginator2( RenderTargetBitmap headerBmp
                          , RenderTargetBitmap contentBmp
                          , Size pageSize
                          , Thickness? margin = null
                          , double contentHeaderHeight = 0)
    : this(headerBmp, contentBmp, null, pageSize, margin, contentHeaderHeight)
    {
    }

    public BitmapPaginator2( RenderTargetBitmap headerBmp
                          , RenderTargetBitmap contentBmp
                          , RenderTargetBitmap footerBmp
                          , Size pageSize
                          , Thickness? margin = null
                          , double contentHeaderHeight = 0)
    {
        _headerBmp  = headerBmp;
        _contentBmp = contentBmp;
        _footerBmp  = footerBmp;

        _pageSize = pageSize;
        _margin   = margin ?? new Thickness(20, 20, 20, 20);

        _headerHeight = headerBmp?.PixelHeight ?? 0;
        _footerHeight = footerBmp?.PixelHeight ?? 0;

        _contentHeaderHeight = contentHeaderHeight;
        _pageContentHeight   = _pageSize.Height
                             - _margin.Top
                             - _headerHeight
                             - _contentHeaderHeight
                             - _footerHeight
                             - _margin.Bottom
                         ;
    }

    public override bool                     IsPageCountValid => true;

    public override int                      PageCount        => (int)Math.Ceiling((_contentBmp.PixelHeight - _contentHeaderHeight) / _pageContentHeight);

    public override IDocumentPaginatorSource Source           => null;

    public override Size PageSize
    {
        get => _pageSize;
        set => throw new NotSupportedException();
    }

    public override DocumentPage GetPage(int pageNumber)
    {
        double yOffsetBmp  = pageNumber * _pageContentHeight
                           + _contentHeaderHeight;
        double yOffsetPage =_margin.Top;
        var    canvas      = new Canvas
                             {
                                 Width  = _pageSize.Width
                               , Height = _pageSize.Height
                             };

        //
        // Header
        //
        if (_headerBmp    != null
        &&  _headerHeight >  0)
            addToCancvas( _headerHeight
                        , CreateImageRegion( _headerBmp
                                           , new Rect(0, 0, _pageSize.Width, _headerHeight)));

        //
        // CONTENT (pagineret)
        //
        if (_contentBmp != null)
        {
            // Content Header
            if (_contentHeaderHeight >  0)
                addToCancvas( _contentHeaderHeight
                            , CreateImageRegion( _contentBmp
                                               , new Rect(0, 0, _pageSize.Width, _contentHeaderHeight)));

            // Content 
            addToCancvas( _pageContentHeight
                        , CreateImageRegion( _contentBmp
                                           , new Rect(0, yOffsetBmp, _pageSize.Width, _pageContentHeight)));
        }

        //
        // FOOTER
        //
        if (_footerBmp    != null
        &&  _footerHeight >  0)
            addToCancvas( _footerHeight, CreateImageRegion( _footerBmp
                                                          , new Rect(0, 0, _pageSize.Width, _footerHeight)));

        //
        // Layout pass (kritisk for XPS)
        //
        canvas.Measure(_pageSize);
        canvas.Arrange(new Rect(new System.Windows.Point(), _pageSize));
        canvas.UpdateLayout();

        return new DocumentPage(canvas);

        // --------------------------------------------------------------------------
        // Helper function to add an image to the canvas and update the yOffsetPage -
        // --------------------------------------------------------------------------
        void addToCancvas(double height, Image image)
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
            dc.DrawImage( bmp
                        , new Rect(-region.X, -region.Y, bmp.PixelWidth, bmp.PixelHeight));
        }

        var croppedBmp = new RenderTargetBitmap( (int)region.Width
                                               , (int)region.Height
                                               , 96, 96
                                               , PixelFormats.Pbgra32);

        croppedBmp.Render(dv);
        croppedBmp.SaveDebugBitmap();
        return new Image
               {
                   Source = croppedBmp
                 , Width  = region.Width
                 , Height = region.Height
               };
    }
}
