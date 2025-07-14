using Caliburn.Micro;


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
            var screen = IoC.Get<ControlViewModel>();            
            await ActivateItemAsync(screen);            
        }
        #endregion
    }
}