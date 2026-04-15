using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using AppArguments;
using DBF.Helpers;
using GithubTools;
using Microsoft.VisualBasic.Devices;
using Syncfusion.UI.Xaml.Maps;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
            //Debugger.Break();

            Arguments.Parse(validArgs, requiredArgs, showHelp);

#if (RELEASE || PRODTEST)
            if (Arguments.Values.Lookup("mode") == "restart")
                _github.MarkAppStarted();
            else
                _github.UpdateAndMarkAppStarted(Arguments.DebugMode);
#endif
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _github.MarkAppExitedNormally();
            base.OnExit(e);
        }

        #region Argument handling
            private static Dictionary<string, string[]> validArgs = new Dictionary<string, string[]>
                                            {     {"mode",  new[] { "normal", "restart"}}
                                                , {"debug", new[] { "false", "true","" }}
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
          mode    : normal | restart 
                     normal  : Normal start of program               (default)
                     restart : Restart using last saved timer states

          debug   : true | false (default=false)                    - Only for testing, so debugger can be attached

          Example:
            DBF.exe mode:restart debug=true";

                if (string.IsNullOrWhiteSpace(msg))
                    new MessageWindow(        helpText).ShowDialog();
                else
                    new MessageWindow(msg + "\n\n" + helpText).ShowDialog();

                if (exitProgram)
                    Environment.Exit(1);
            }
        #endregion
    }
}
