using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Views;
using GithubTools;

namespace DBF.ViewModels;

public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
{
    private IWindowManager windowManager;
    public Configuration Configuration { get; set; }

    public ShellViewModel(Configuration configuration, IWindowManager windowManager)
    {
        Configuration = configuration;
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
            _github.Update(             "install");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Updater Error: {ex.Message}", "Updater Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }
    }

    public async void ShowAbout()
    {
        var viewModel = IoC.Get<AboutViewModel>();
        await windowManager.ShowDialogAsync(viewModel);
    }

    public void Restart()
    {
        try
        {
            Configuration.SaveState();

            // Best-effort for at finde den reelle exe (ikke en DLL)
            string exePath = null;

            try
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            catch { /* adgang/permission kan fejle i visse miljøer */ }

            if (string.IsNullOrEmpty(exePath))
            {
                var args0 = Environment.GetCommandLineArgs().FirstOrDefault();

                if (!string.IsNullOrEmpty(args0))
                    exePath = Path.GetFullPath(args0);
            }

            if (string.IsNullOrEmpty(exePath))
                exePath = Assembly.GetEntryAssembly()?.Location;

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("Kan ikke finde applikationens eksekverbare fil til genstart.", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var psi = new ProcessStartInfo
                      {
                          FileName         = exePath
                        , Arguments        = "mode:restart"
                        , UseShellExecute  = false
                        , WorkingDirectory = Environment.CurrentDirectory
                      };
                            
            var process=Process.Start(psi);
            //process.WaitForExit();

            var text= $"FileName         : {psi.FileName}\r\n"
                    + $"Arguments        : {psi.Arguments}\r\n"
                    + $"UseShellExecute  : {psi.UseShellExecute}\r\n"
                    + $"WorkingDirectory : {psi.WorkingDirectory}";
                    //+ $"exitcode         : {process.ExitCode}";

            File.WriteAllText(@"d:\DBFtools.log", text);
        }

        catch (Exception ex)
        {
            MessageBox.Show($"Genstart mislykkedes: {ex.Message}", "Fejl", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Environment.Exit(0);
    }
}
