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
using String.Localization;

namespace DBF.ViewModels;

public class ShellViewModel : Conductor<Screen>.Collection.OneActive, IConductActiveItem
{
    private IWindowManager windowManager;
    public bool IsFullscreen { get; set; }
    private WindowState _previousWindowState;
    private WindowStyle _previousWindowStyle;
    private ResizeMode  _previousResizeMode;
    private Rect        _previousBounds;
    public Visibility    FullScreenVisibility => IsFullscreen
                                               ? Visibility.Collapsed
                                               : Visibility.Visible;

    public Visibility    ListsVisibility      => (!IsFullscreen && Configuration.ReadBC3)
                                               ? Visibility.Visible
                                               : Visibility.Collapsed;

    public Configuration Configuration        { get; set; }

    public ShellViewModel(Configuration configuration, IWindowManager windowManager)
    {
        this.windowManager = windowManager;
        Configuration      = configuration;
    }

    #region Show Screens
        public async void OpenControlView()
        {
            await Configuration.LoadAsync();

            Configuration.PropertyChanged+= (s, e) =>
            {
                if (e.PropertyName == nameof(Configuration.ReadBC3))
                    NotifyOfPropertyChange(nameof(ListsVisibility));
            };

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
        var name = Lex.Culture?.Name is "da-DK" or "da" ? "en-US" : "da-DK";

        // 1. Save configuration
        Configuration.CultureName = name;
        Configuration.Save();

        // 2. Set culture first
        LanguageService.Instance.SetCulture(name);

        // 3. Update UI on all views
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
        window.Show();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        bool isUiThread = Application.Current.Dispatcher.CheckAccess();

        if (e.Key == Key.F11)
            ToggleFullScreen();
        else
            if (IsFullscreen
            &&  e.Key == Key.Escape)
                ToggleFullScreen();
    }

    public void ToggleFullScreen()
    {
        var window = Application.Current.MainWindow;

        if (window is ShellView view)
            view.ToggleMaximize(IsFullscreen = !IsFullscreen);
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
            var cultureName = LanguageService.Instance.CurrentCulture.Name;

            Logger.Info("Running GitHup Updater");
            GitHub _github = new GitHub("DBF");
            _github.Update(Arguments.DebugMode, cultureName, "install");
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

            // Best-effort on finding the actual exe (not a DLL)
            string exePath = null;

            try
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName;
            }

            catch { /* permission can fail in certain environments */ }

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

    public void Minimize()
    {
        (GetView() as Window).WindowState = WindowState.Minimized;
    }

    public async Task CloseAsync()
    {
        try
        {
            await TryCloseAsync();
        }

        catch (Exception ex)
        {
            Debug.WriteLine($"{ex.Message}", Lex.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void TitleBarDrag(MouseButtonEventArgs e)
    {
        var win = (Window)GetView();

        if (e.ClickCount == 2)
        {
            // Toggle maximize
            win.WindowState = win.WindowState == WindowState.Maximized
                            ? WindowState.Normal
                            : WindowState.Maximized;
        }
        else
            win.DragMove();
    }
}

