using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;

namespace DBF.ViewModels
{
    public class PresetNameViewModel : Screen
    {
        public readonly Configuration Configuration;

        public PresetNameViewModel(Configuration configuration)
        {
            Configuration = configuration;
        }

        public string PresetName { get; set; }

        public async Task CancelInput()
        {
            PresetName = null;
            await TryCloseAsync();
        }

        public async Task ConfirmInput()
        {
            if (string.IsNullOrEmpty(PresetName))
                MessageBox.Show(Lex.NameIt+"!", Lex.Settings, MessageBoxButton.OK, MessageBoxImage.Information);
            else
                if (Configuration.Presets.FirstOrDefault(p => p.Name == PresetName) != null)
                    MessageBox.Show(Lex.PresetAlreadyExists, Lex.Settings, MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    await TryCloseAsync();
        }
    }
}
