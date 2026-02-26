using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Views;
using GithubTools;

namespace DBF.ViewModels;

    public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
    {
        private IWindowManager windowManager;
         public Configuration Configuration{ get; set; }

        public ShellViewModel(Configuration configuration, IWindowManager windowManager)
        {
           Configuration= configuration;
            configuration.Load();
            this.windowManager = windowManager;
        }

        #region Show Screens
            public async void OpenControlView()
            {
                var viewModel = IoC.Get<ControlViewModel>();
                await ActivateItemAsync(viewModel);
            }
        #endregion

        public async Task OpenSettingsAsync()
        {
            var viewModel = IoC.Get<ConfigurationViewModel>();
            await windowManager.ShowDialogAsync(viewModel);
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
            var viewModel = IoC.Get<AboutViewModel>();
            await windowManager.ShowDialogAsync(viewModel);
        }
    }
