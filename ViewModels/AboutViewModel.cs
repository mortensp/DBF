using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Caliburn.Micro;

namespace DBF.ViewModels
{
    public class AboutViewModel : Screen
    {
        public string AppName=> "DBF Tools";
        public string Version=> "v" + Assembly.GetExecutingAssembly().GetName().Version;
        public string Author => "Morten Sparding";
        public string Description => @"Dette er et simpelt hjælpe program, som arbejder samme med BC3's output. "
                                   + @"Dvs. at Start og Resultatlister hentes fra de data, som er sendt eller sendes til hjemmesiden. "
                                   + Environment.NewLine
                                   + @"Bridgeurene er dog uafhængige af BC3. ";

        public async void Close()
        {
            await TryCloseAsync();
        }
    }
}
