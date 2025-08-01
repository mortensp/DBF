using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using Baksteen.Extensions.DeepCopy;
using Caliburn.Micro;
using DBF.DataModel;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.ViewModels
{
    public class ConfigurationViewModel : Screen
    {
        private Configuration configuration;

        #region Constructors
            public ConfigurationViewModel(Configuration configuration)
            {
                this.configuration = configuration;
                NewConfiguration.Update(configuration);
            }
        #endregion

        #region public Properties  
            public Configuration NewConfiguration { get; set; } = new();
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                await TryCloseAsync();
                configuration.Update(NewConfiguration);
                configuration.Save();
            }
        #endregion
    }
}