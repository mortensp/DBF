using System.Printing;
using System.Windows;


namespace DBF.Services;

public class PrintSettings
{
    public Size PageSize { get; set; } = new Size(793, 1122);
    public Thickness Margin { get; set; } = new Thickness(40);
    public double Scale { get; set; } = 1.0;
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;
}
