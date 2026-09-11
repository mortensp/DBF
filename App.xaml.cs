using System.Configuration;
using System.Windows;
using AppArguments;
using Caliburn.Micro;
using DBF.Helpers;
using GitHubTools;
using String.Localization;
using static PrintDialogX.InterfaceSettings;

namespace DBF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private GitHub _github = new GitHub("DBF");

        protected override void OnStartup(StartupEventArgs e)
        {
#if (RELEASE || PRODTEST)
            // How to run the app
            var mode = Arguments.Values.Lookup("mode");

            if (Arguments.Values.Lookup("mode") == "restart")
            {
                Logger.Info("Performing a Restart");
                _github.MarkAppStarted();
            }
            else
            {
                Logger.Info("Looking for new version online");
                var cultureName = LanguageService.Instance.CurrentCulture;
                _github.UpdateAndMarkAppStarted(Arguments.DebugMode, cultureName.Name);
            }
#else
            _github.MarkAppStarted();
#endif

            var fontService             = IoC.Get<FontSizeService>();
            fontService.PropertyChanged+= (s, args) =>
            {
                if (args.PropertyName == nameof(FontSizeService.FontSize))
                {
                    Application.Current.Resources["MediumFontSize"]  = fontService.FontSize;
                    Application.Current.Resources["LargeFontSize"]   = fontService.FontSize + 2;
                    Application.Current.Resources["X-LargeFontSize"] = fontService.FontSize + 4;
                }
            };

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _github.MarkAppExitedNormally();
            base.OnExit(e);
        }
    }
}
