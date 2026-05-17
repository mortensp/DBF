using System.Reflection;
using Caliburn.Micro;
using WPFLocalizeExtension.Engine;
using DBF.Localization;

namespace DBF.ViewModels;

public class AboutViewModel : Screen
{
    public string AppName=> "DBF Tools";
    public string Version=> "v" + Assembly.GetExecutingAssembly().GetName().Version;
    public string Author => "Morten Sparding";


    public async void Close()
    {
        await TryCloseAsync();
    }
}
