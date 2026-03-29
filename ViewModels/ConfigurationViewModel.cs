using Caliburn.Micro;
using DBF.DataModel;

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

                //if (NewConfiguration.StartTime is null)
                //NewConfiguration.StartTime = new TimeOnly(19, 0, 0);
            }
        #endregion

        #region public Properties  
            public Configuration NewConfiguration { get; set; }
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                var old =configuration.ReadBC3 ;

                await TryCloseAsync();

                configuration.Update(NewConfiguration);
                configuration.Save();

                if (old != NewConfiguration.ReadBC3)
                    IoC.Get<ControlViewModel>()
                       .SetWatcher(NewConfiguration.ReadBC3);
            }
        #endregion
    }
}
