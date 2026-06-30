using System.ComponentModel;
using Caliburn.Micro;

namespace DBF.Helpers;

public class FontSizeService : PropertyChangedBase
{
    private double _fontSize = 12;
    public double FontSize
    {
        get => _fontSize;
        set => Set(ref _fontSize, value);
    }
}
