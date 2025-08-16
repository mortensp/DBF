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
        public ShellViewModel(Configuration configuration)
        {
            configuration.Load();
        }

        #region Show Screens
            public async void OpenControlView()
            {
#if RELEASE
            var updater = new UpdateManager("mortensp", "DBF");
            await updater.CheckForUpdateAsync();
#endif
            var screen = IoC.Get<ControlViewModel>();
            await ActivateItemAsync(screen);
        }
        #endregion

        public async Task OpenSettingsAsync()
        {
            // Åbn indstillinger
            var screen = IoC.Get<ConfigurationViewModel>();
            var windowManager = IoC.Get<IWindowManager>();
            
            await windowManager.ShowDialogAsync(screen);
        }

 

        public void TimersHelp()
        {
            var window = new TimersHelpWindow(); // Views\TimersHelpWindow.xaml
            window.ShowDialog();
        }
    }
}