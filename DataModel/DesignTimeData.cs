using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using Syncfusion.Presentation;
using PropertyChanged;

namespace DBF.DataModel;

public class DesignTimeData : PropertyChangedBase
{
    public DesignTimeData()
    {
        Visibility       = Visibility.Visible;

        Timer = new BridgeTimer
        {
            Visibility = Visibility.Visible,
            Background = System.Windows.Media.Brushes.Orange,
            Time = "21:17",
            RoundText = "3. Runde",
            Info = "Vi spiller 7 runder af 24 spil",
            MoreInfo = "Pause efter 4. Runde",
            MinutesLeft = 13d,
            WarningVisiblity = Visibility.Visible
        };

        Configuration = new Configuration() { StartTime = DateTime.Now };
        Configuration.Load();

    }

    public string        RoundText     { get; set; } = "Design-time: Round 2";
    public BridgeTimer   Timer         { get; set; }
    public Visibility    Visibility    { get; set; }
    public Configuration Configuration { get; set; }
}
