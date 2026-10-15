using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.HelpSystem;

public static class HelpProvider
{
    public static HelpContent Get(string key)
    {
        return key switch
        {
            "AppSettings"   => new HelpContent( Title: LocHelp.AppSettings_Title
                                              , Text: null
                                              , Image: LocHelp.AppSettings_Image
                                              , Width: 760
                                              , Height: 1083
                                              ),

            "TimerSettings" => new HelpContent( Title: LocHelp.TimerSettings_Title
                                              , Text: null
                                              , Image: LocHelp.Timersettings_Image
                                              , Width: 760
                                              , Height: 1083
                                              ),
            "BridgeTimer"   => new HelpContent( Title: LocHelp.BridgeTimer_Title
                                              , Text: null
                                              , Image: LocHelp.BridgeTimer_Image
                                              , Width: 532
                                              , Height: 675
                                              ),

            _               => new HelpContent( Title: LocHelp.Unknown_Help_Title
                                              , Text: null
                                              , Image: LocHelp.Unknown_Help_Image
                                              , Width: 600
                                              , Height: 300
                                              , IsMissing: true
                                              )
        };//
    }
}
