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
            "AppSettings" => new HelpContent(
                Title: LocHelp.AppSettings_Title,
                Text: null,
                Image: LocHelp.AppSettings_Image
            ),

            "TimerSettings" => new HelpContent(
                Title: LocHelp.TimerSettings_Title,
                Text: null,
                Image: LocHelp.Timersettings_Image
            ),
                  
            _ => new HelpContent(
                Title: LocHelp.Unknown_Help_Image,
                Text: null,
                Image: null
            )
        };//
    }
}
