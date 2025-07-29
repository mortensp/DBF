using System.Reflection;
using Caliburn.Micro;
using Github;

namespace DBF.ViewModels
{
    public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
    {
        public ShellViewModel()
        {
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
    }
}