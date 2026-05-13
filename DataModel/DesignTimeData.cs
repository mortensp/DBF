using System.Windows;
using Caliburn.Micro;

namespace DBF.DataModel;

public class DesignTimeData : PropertyChangedBase
{
    public DesignTimeData()
    {
        Visibility = Visibility.Visible;

        Timer = new BridgeTimer
                {
                    Visibility         = Visibility.Visible
                  ,   Background       = System.Windows.Media.Brushes.Orange
                  ,   Time             = "21:17"
                  ,   RoundText        = "3. Runde"
                  ,   Info             = "Vi spiller 7 runder af 24 spil"
                                       + Environment.NewLine + "Pause efter 4. Runde"
                  ,   MinutesLeft      = 13d
                  ,   WarningVisiblity = Visibility.Visible
                };

        Configuration           = IoC.Get<Configuration>();
        Configuration.StartDate = DateTime.Now;
        _=Configuration.LoadAsync();
    }

    public string        RoundText     { get; set; } = "Design-time: Round 2";
    public BridgeTimer   Timer         { get; set; }
    public Visibility    Visibility    { get; set; }
    public Configuration Configuration { get; set; }
}
