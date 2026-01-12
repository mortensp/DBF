using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.UserControls;
using DBF.Views;
using GithubTools;
using Microsoft.EntityFrameworkCore.Update.Internal;


//using Github;
using static System.TimeZoneInfo;

namespace DBF.ViewModels
{
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
    {
        private IWindowManager windowManager;

        public ShellViewModel(Configuration configuration, IWindowManager windowManager)
        {
            configuration.Load();
            this.windowManager = windowManager;
        }

        #region Show Screens
            public async void OpenControlView()
            {
                var screen = IoC.Get<ControlViewModel>();
                await ActivateItemAsync(screen);
            }
        #endregion

        public async Task OpenSettingsAsync()
        {
            var screen = IoC.Get<ConfigurationViewModel>();
            await windowManager.ShowDialogAsync(screen);
        }

        public void TimersHelp()
        {
            var window = new TimersHelpWindow();
            window.ShowDialog();
        }

        public void Install()
        {
            try
            {
                Github _github = new Github("DBF");
                _github.Update("install");
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Updater Error: {ex.Message}" , "Updater Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }

        public async void ShowAbout()
        {
            var screen = IoC.Get<AboutViewModel>();
            await windowManager.ShowDialogAsync(screen);
        }
    }
}
