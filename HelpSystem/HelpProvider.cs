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
                                              , Width: 850
                                              , Height: null
                                              ),

            "TimerSettings" => new HelpContent( Title: LocHelp.TimerSettings_Title
                                              , Text: null
                                              , Image: LocHelp.Timersettings_Image
                                              , Width: 909
                                              , Height: null
                                              ),

            _               => new HelpContent( Title: LocHelp.Unknown_Help_Image
                                              , Text: null
                                              , Image: null
                                              )
        };//
    }
}
