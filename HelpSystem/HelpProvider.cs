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

            //"ResetTimers" => new HelpContent(
            //    Title: HelpTexts.ResetTimers_Title,
            //    Text: HelpTexts.ResetTimers_Text,
            //    Image: HelpImages.Help_ResetTimers
            //),

            //"Overview" => new HelpContent(
            //    Title: HelpTexts.Overview_Title,
            //    Text: HelpTexts.Overview_Text,
            //    Image: HelpImages.Help_Overview
            //),

            _ => new HelpContent(
                Title: "Hjælp",
                Text: "Ingen hjælp fundet for denne funktion.",
                Image: null
            )
        };//
    }
}
