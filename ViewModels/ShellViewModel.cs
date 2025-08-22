using System.Reflection;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Views;
using Github;
using static System.TimeZoneInfo;

namespace DBF.ViewModels
{
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
    {
        IWindowManager windowManager;

        public ShellViewModel(Configuration configuration, IWindowManager windowManager)
        {
            configuration.Load();
            this.windowManager = windowManager;
        }

        #region Show Screens
            public async void OpenControlView()
            {
#if RELEASE
            var updater = new UpdateManager("mortensp", "DBF");
            await updater.CheckForUpdateAsync();

            AccessInstaller.CheckAndInstall();
            
#endif
            var screen = IoC.Get<ControlViewModel>();
            await ActivateItemAsync(screen);
        }
        #endregion

        public async Task OpenSettingsAsync()
        {
            // Åbn indstillinger
            var screen = IoC.Get<ConfigurationViewModel>();
            
            await windowManager.ShowDialogAsync(screen);
        }

 
        public void TimersHelp()
        {
            var window = new TimersHelpWindow(); // Views\TimersHelpWindow.xaml
            window.ShowDialog();
        }

        public async void ShowAbout()
        {
            //var window = new AboutView();
            //window.ShowDialog();

            
            var screen = IoC.Get<AboutViewModel>();

            await windowManager.ShowDialogAsync(screen);
        }

    }
}