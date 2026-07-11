using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.DataModel;
using DBF.Helpers;
using DBF.ViewModels;
using DBF.Views;
using String.Localization;

namespace DBF;

//[TraceOn()]
public class Bootstrapper : BootstrapperBase
{
    #region Constructors
        public Bootstrapper()
        {
            Logger.Info("Bootstrapper initializing");

            // Load arguments 
            AppArguments.Load();
            LanguageService.Instance.Initialize( typeof(Lex)
                                               , typeof(LocHelp)
                                               , typeof(Syncfusion_Shared_Wpf)
                                               , typeof(Syncfusion_SfColorPalette_Wpf));
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            Initialize();

            // Load language setting first - before anything else
            IoC.Get<Configuration>().LoadLanguageSetting();

            Logger.Info("Bootstrapper initializer");
        }
    #endregion

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
#if false
        DisplayRootViewForAsync<TimerSettingsViewModel>();
        var screen = IoC.Get<TimerSettingsViewModel>();
#else
        // Show screen at startup
        DisplayRootViewForAsync<ShellViewModel>();

        IoC.Get<ShellViewModel>().OpenControlView();
#endif
        // // Restore Taskbar Icon.
        // Application.MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/DBF_Tools.ico", UriKind.Absolute));
    }

    protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Exception(e.Exception, "Unhandled Exception in Bootstrapper");

        if (e.Exception.Message.Equals("No target found for method Close."))
            Environment.Exit(0);

        base.OnUnhandledException(sender, e);
    }

    #region SimpleContainer Overrides and Configuration.
        private readonly SimpleContainer _container = new();

        [DebuggerStepThrough]
        protected override void Configure()
        {
            SyncFusion.FindandRegisterLicenseKey();

            _container.Singleton<IWindowManager, ZoomWindowManager>();
            _container.Singleton<IEventAggregator, EventAggregator>();
            _container.Singleton<Configuration>();
            _container.Singleton<BridgeMate>();
            _container.Singleton<IAudioService, WindowsAudioService>();
            _container.Singleton<FontSizeService>();

            foreach (var viewModel in SelectViewModels())
                if (_container.HasHandler(viewModel, null) == false)
                    if (viewModel.Name == "ShellViewModel"
                    ||  viewModel.Name == "ControlViewModel")
                    {
                        _container.RegisterSingleton(viewModel, null, viewModel);
                        Logger.Debug($"Registered {viewModel.Name} as Singleton");
                    }
                    else
                    {
                        _container.RegisterPerRequest(viewModel, null, viewModel);
                        Logger.Debug($"Registered {viewModel.Name} as PerRequest");
                    }

            var defaultLocateTypeForModelType = ViewLocator.LocateTypeForModelType;

            ViewLocator.LocateTypeForModelType = FindTypeForModelType(defaultLocateTypeForModelType);

            Logger.Info("Bootstrapper configured");
        }

        [DebuggerStepThrough]
        private static Func<Type, DependencyObject, object, Type> FindTypeForModelType(Func<Type, DependencyObject, object, Type> defaultLocateTypeForModelType)
        {
            return (Type modelType, DependencyObject displayLocation, object context) =>
                   {
                       if (modelType == typeof(ControlViewModel))
                           if (context  is string viewName
                           &&  viewName == "ProjectorView")
                               return typeof(ProjectorView);

                       return defaultLocateTypeForModelType(modelType, displayLocation, context);
                   };
        }

        [DebuggerStepThrough]
        protected override object GetInstance(Type service, string key)
        {
            var instance = _container.GetInstance(service, key);

            if (instance != null)
                return instance;

            throw new InvalidOperationException("Could not locate any instances.");
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            _container.BuildUp(instance);
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            // We pick one type from each assembly to get to the assemblies
            yield return Assembly.GetAssembly(typeof(Bootstrapper));
        }

        protected IEnumerable<Type> SelectViewModels()
        {
            return SelectAssemblies().SelectMany(a => a.GetTypes())
                                     .Where(t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));
        }
    #endregion
}
