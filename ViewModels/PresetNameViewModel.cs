using Caliburn.Micro;
using System.Windows;
using DBF.DataModel;

namespace DBF.ViewModels
{
    public class PresetNameViewModel : Screen
    {
        //private WindowManager _windowManager = IoC.Get<WindowManager>();
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
                MessageBox.Show("Du skal angive et navn til de nye indstillinger!", "Indstillinger", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                if (Configuration.Presets.FirstOrDefault(p => p.Name == PresetName) != null)
                    MessageBox.Show("Der findes allerede en indstilling med det navn!", "Indstillinger", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    await TryCloseAsync();
        }
    }
}