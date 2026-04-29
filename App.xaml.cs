using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using AppArguments;
using DBF.Helpers;
using DBF.Resources;
using GithubTools;
using WPFLocalizeExtension.Engine;
using Localization;
using Microsoft.VisualBasic.Devices;
using Syncfusion.UI.Xaml.Maps;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Syncfusion.Windows.Edit;
using WPFLocalizeExtension.Providers;

namespace DBF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Github _github = new Github("DBF");

        protected override void OnStartup(StartupEventArgs e)
        {
            Arguments.Parse(validArgs, requiredArgs, showHelp);
            Logger.Info($"Application arguments: {Arguments.Values.ToFormattedString()}");

            // Initialize LanguageService
            var asmName = typeof(Resources.Strings).Assembly.GetName().Name;
            LanguageService.Instance.Initialize(asmName, "Strings");
            Strings.Culture = LanguageService.Instance.SetCulture(Arguments.Values.Lookup("language"));
#if DEBUG        
            //Strings.Culture = LanguageService.Instance.SetCulture("en");
#endif
            //base.OnStartup(e);
            
            // How to run the app
            var mode = Arguments.Values.Lookup("mode");

#if (RELEASE || PRODTEST)
            if (Arguments.Values.Lookup("mode") == "restart")
            {
                Logger.Info("Performing a Restart");
                _github.MarkAppStarted();
            }
            else
            {
                Logger.Info("Looking for new version online");
                _github.UpdateAndMarkAppStarted(Arguments.DebugMode);
            }
#else
            _github.MarkAppStarted();
#endif

            base.OnStartup(e);

        }

        protected override void OnExit(ExitEventArgs e)
        {
            _github.MarkAppExitedNormally();
            base.OnExit(e);
        }

        #region Argument handling
            private static ArgumentMap validArgs = 
                                       new ArgumentMap{
                                                            {"mode",  new ( "normal", "restart", "reset") }
                                                          , {"debug", new ("false", "true","" ) }
                                                      };

            private static string[] requiredArgs = new string[0];

            private static void showHelp(string msg, bool exitProgram)
            {
                string helpText = @"
                🔧 DBF Tools Help
        ====================
        Usages:
          DBF.exe [mode:MyMode]  

        Parameters:
          mode    : normal | restart | reset
                     normal  : Normal start of program               (default)
                     restart : Restart using last saved timer states
                     reset   : Clear all settings and make a fresh restart

          debug   : true | false (default=false)                    - Only for testing, so debugger can be attached

          Example:
            DBF.exe mode:restart debug=true";

                if (string.IsNullOrWhiteSpace(msg))
                    new MessageWindow(helpText).ShowDialog();
                else
                    new MessageWindow(msg + "\n\n" + helpText).ShowDialog();

                if (exitProgram)
                    Environment.Exit(1);
            }
        #endregion
    }
}
