using System.Windows;
using System.Windows.Controls;

namespace PrintWPF.UserControls;

public class CustomDocumentViewer : DocumentViewer
{
    static CustomDocumentViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata( typeof(CustomDocumentViewer)
                                                , new FrameworkPropertyMetadata(typeof(CustomDocumentViewer)));
    }

    public CustomDocumentViewer() : base()
    {
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Zoom +
        if (GetTemplateChild("PART_ZoomPlus") is Button zoomPlus)
            zoomPlus.Click += (s, e) => IncreaseZoom();

        // Zoom -
        if (GetTemplateChild("PART_ZoomMinus") is Button zoomMinus)
            zoomMinus.Click += (s, e) => DecreaseZoom();
    }
}
