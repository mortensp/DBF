using System.Configuration;
using System.Data;
using System.Windows;
using DBF.Helpers;
using GithubTools;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DBF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Github _github = new Github("DBF");

        protected override void OnStartup(StartupEventArgs e)
        {      
            _github.UpdateAndMarkAppStarted();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _github.MarkAppExitedNormally();
            base.OnExit(e);
        }
    }
}
