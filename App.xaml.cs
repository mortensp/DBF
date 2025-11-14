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
        protected override void OnStartup(StartupEventArgs e)
        {
#if RELEASE
            
            Github.Update();
#endif
            CrashDetector.MarkAppStarted();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            CrashDetector.MarkAppExitedNormally();
            base.OnExit(e);
        }
    }
}
