using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.DataModel;

public static class Global
{
    public static readonly CultureInfo UsCulture = new("en-US");
    public static readonly CultureInfo DkCulture = new("da-DK");

    public static ObservableCollection<CustomColor> GetStandardColors()
    {
        return new()
        {
              new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FFFFFF"),   ColorName = Lex.Color_White }
            , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#F2460D"),   ColorName = Lex.Color_Red }
            , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FF66CCFF"), ColorName = Lex.Color_Blue }
            , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FF9D00"),   ColorName = Lex.Color_Orange }
            , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#81C784"),   ColorName = Lex.Color_Green }
            , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#000000"),   ColorName = Lex.Color_Black }
  
            };
    }


}

