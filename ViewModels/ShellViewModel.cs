using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using AppArguments;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.Views;
using GitHubTools;
using Localization;
//using Strings = DBF.Properties.Strings;
namespace DBF.ViewModels;

public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
{
    private IWindowManager windowManager;
    private bool _isFullscreen { get; set; }
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode  _previousResizeMode;
    private Rect        _previousBounds;
    public Visibility    FullScreenMode => _isFullscreen ? Visibility.Collapsed : Visibility.Visible;

    public Configuration Configuration  { get; set; }

    public ShellViewModel(Configuration configuration, IWindowManager windowManager)
    {
        this.windowManager = windowManager;
        Configuration      = configuration;
    }

    #region Show Screens
        public async void OpenControlView()
        {
            await Configuration.LoadAsync();
            Logger.Info("Opening ControlViewModel");
            var viewModel = IoC.Get<ControlViewModel>();
            await ActivateItemAsync(viewModel);
        }
    #endregion

    public async Task OpenSettingsAsync()
    {
        var viewModel = IoC.Get<ConfigurationViewModel>();
        await windowManager.ShowDialogAsync(viewModel);
    }

    public async Task ToggleLanguageAsync()
    {
        var name = Lex.Culture?.Name is "da-DK" or "da" ? "en" : "da-DK";

        // 1. Gem konfiguration
        Configuration.CultureName = name;
        Configuration.Save();

        // 2. Sæt kultur først
        // Lex.Culture = LanguageService.Instance.SetCulture(name);
        LanguageService.Instance.SetCulture(name);

        // 3. Opdater UI på alle views
        IoC.Get<ControlViewModel>()?.LexRefresh();
    }

    public async Task Test()
    {
        var cvm = IoC.Get<ControlViewModel>();
        cvm.Test();
    }

    public void TimersHelp()
    {
        var window = new TimersHelpWindow();
        window.ShowDialog();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F11)
            ToggleFullscreen();
    }

    public void ToggleFullscreen()
    {
        var vindow = Application.Current.MainWindow;

        if (_isFullscreen)
        {
            // Gendan tidligere tilstand
            vindow.WindowStyle = _previousWindowStyle;
            vindow.ResizeMode  = _previousResizeMode;

            vindow.WindowState = WindowState.Normal;

            vindow.Left        = _previousBounds.Left;
            vindow.Top         = _previousBounds.Top;
            vindow.Width       = _previousBounds.Width;
            vindow.Height      = _previousBounds.Height;
            vindow.WindowState = _previousWindowState;

            _isFullscreen = false;
        }
        else
        {
            // Gem nuværende tilstand
            _previousWindowState = vindow.WindowState;
            _previousWindowStyle = vindow.WindowStyle;
            _previousResizeMode  = vindow.ResizeMode;
            _previousBounds      = new Rect(vindow.Left, vindow.Top, vindow.Width, vindow.Height);

            // Gå i fullscreen
            vindow.WindowStyle = WindowStyle.None;
            vindow.ResizeMode  = ResizeMode.NoResize;
            vindow.WindowState = WindowState.Normal; // vigtigt for korrekt max
            vindow.WindowState = WindowState.Maximized;

            _isFullscreen = true;
        }
    }

    public void OpenSettingFiles()
    {
        Configuration.OpenJSONFiles();
    }

    public void OpenLogFile()
    {
        Configuration.OpenLogFile();
    }

    public void Install()
    {
        try
        {
            Logger.Info("Running Githup Updater");
            GitHub _github = new GitHub("DBF");
            _github.Update(Arguments.DebugMode, "install");
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
                MessageBox.Show($"{Lex.ErrorExecutableFileMissing}.", Lex.Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var psi = new ProcessStartInfo
                      {
                          FileName         = exePath
                        , Arguments        = "mode:restart"
                        , UseShellExecute  = false
                        , WorkingDirectory = Environment.CurrentDirectory
                      };

            var process =Process.Start(psi);
        }

        catch (Exception ex)
        {
            MessageBox.Show($"{Lex.ErrorRestart}: {ex.Message}", Lex.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Environment.Exit(0);
    }
}
