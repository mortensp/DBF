using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;

namespace DBF.ViewModels
{
    public class ConfigurationViewModel : Screen
    {
        private Configuration configuration;

        #region Constructors
            public ConfigurationViewModel(Configuration configuration)
            {
                this.configuration = configuration;
                NewConfiguration   = new();
                NewConfiguration.Update(configuration);
            }
        #endregion

        #region public Properties  
            public Configuration NewConfiguration { get; set; }
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                configuration.Save();
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                var readBC3        =configuration.ReadBC3 ;
                var readBridgeMate =configuration.ReadBridgeMate ;

                configuration.Update(NewConfiguration);
                configuration.Save();

                Logger.Debug("App settings changed");

                if (configuration.IsLoaded)
                {
                    var vm =IoC.Get<ControlViewModel>();

                    if (readBC3 != NewConfiguration.ReadBC3)
                        vm?.SetBC3Watcher(NewConfiguration.ReadBC3);

                    if (readBridgeMate != NewConfiguration.ReadBridgeMate)
                        vm?.SetBridgeMateWatcher(NewConfiguration.ReadBridgeMate);
                }

                await TryCloseAsync();
            }
        #endregion
    }
}
