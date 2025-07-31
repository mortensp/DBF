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

        #region Constructors
            public ConfigurationViewModel(Configuration configuration)
            {
                Configuration = configuration;
                NewConfiguration = Configuration.DeepCopy();
            }
        #endregion

        #region public Properties  
            public Configuration Configuration { get; private set; }
            public Configuration NewConfiguration { get; set; }
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                await TryCloseAsync();
                Configuration.Update(NewConfiguration);
                Configuration.Save();
            }
        #endregion
    }
}