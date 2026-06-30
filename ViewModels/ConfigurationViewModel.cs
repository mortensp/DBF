using System.Globalization;
using System.Text.RegularExpressions;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.Localization;
using EntityFrameworkCore.Jet.Data;
using Microsoft.DotNet.DesignTools.Protocol.Values;
using String.Localization;
using Syncfusion.Windows.Forms;

namespace DBF.ViewModels
{
    public class ConfigurationViewModel : Screen
    {
        private Configuration configuration;

        #region Constructors
            public ConfigurationViewModel(Configuration configuration, FontSizeService fontSizeService)
            {
                this.configuration = configuration;
                FontSizeService    = fontSizeService;
                NewConfiguration   = new();
                NewConfiguration.Update(configuration);
            }
        #endregion

        #region public Properties  
            public  Configuration   NewConfiguration { get; set; }

            private FontSizeService FontSizeService  { get; set; }
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                FontSizeService.FontSize = configuration.FontSize;
                configuration.Save();
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                var culture        = LanguageService.Instance.CurrentCulture;
                var readBC3        =configuration.ReadBC3 ;
                var readBridgeMate =configuration.ReadBridgeMate ;

                configuration.Update(NewConfiguration, true);
                configuration.FontSize = FontSizeService.FontSize;
                configuration.Save();

                Logger.Info("App settings changed");

                if (configuration.IsLoaded)
                {
                    var vm =IoC.Get<ControlViewModel>();

                    if (readBC3 != NewConfiguration.ReadBC3)
                        vm?.SetBC3Watcher(NewConfiguration.ReadBC3);

                    if (readBridgeMate != NewConfiguration.ReadBridgeMate)
                        vm?.SetBridgeMateWatcher(NewConfiguration.ReadBridgeMate);
                }

                if (LanguageService.Instance.CurrentCulture.TwoLetterISOLanguageName != culture.TwoLetterISOLanguageName)
                {
                    Logger.Info($"Restarting application to apply new culture settings.");

                    var shell = IoC.Get<ShellViewModel>();
                    shell.Restart();
                }

                await TryCloseAsync();
            }
        #endregion
    }
}
