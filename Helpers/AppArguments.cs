using AppArguments;
using DBF.Helpers;

namespace DBF;

public static class AppArguments
{
    public static void Load()
    {
        if (Design.IsInDesignMode())
            return;

        Arguments.Parse(validArgs, requiredArgs, showHelp);
        Logger.Info($"Application arguments: {Arguments.Values.ToFormattedString()}");
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

