//using BigBin;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
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
using Localization;
using Microsoft.DotNet.DesignTools.ViewModels;

namespace DBF
{
    //[TraceOn()]
    public class Bootstrapper : BootstrapperBase
    {
        public Bootstrapper()
        {
            Logger.Debug("Bootstrapper initialising");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            FrameworkElement.LanguageProperty
                            .OverrideMetadata( typeof(FrameworkElement)
                                             , new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            //Thread.CurrentThread.CurrentCulture = Global.DkCulture;
            Initialize();
            Logger.Debug("Bootstrapper initialised");
        }

        #region SimpleContainer Overrides and Configuration.
            private readonly SimpleContainer _container = new();

            //[DebuggerStepThrough]
            protected override void Configure()
            {
                SyncFusion.FindandRegisterLicenseKey();

                _container.Instance<IWindowManager>(new WindowManager());
                _container.Singleton<IEventAggregator, EventAggregator>();
                _container.Singleton<Configuration>();
                _container.Singleton<BridgeMate>();
                _container.Singleton<IAudioService, WindowsAudioService>();

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

            protected override IEnumerable<object> GetAllInstances(Type service) => _container.GetAllInstances(service);

            protected override void BuildUp(object instance)
            {
                _container.BuildUp(instance);
            }

            protected override IEnumerable<Assembly> SelectAssemblies()
            {
                // We pick one type from each assembly to get to the assemblies
                yield return Assembly.GetAssembly(typeof(Bootstrapper));
            }

            protected IEnumerable<Type> SelectViewModels() => SelectAssemblies().SelectMany(a => a.GetTypes())
                                                                                .Where(t => t.Name.EndsWith("ViewModel", StringComparison.Ordinal));
        #endregion

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //Logger.Error(e.Exception.Message, "Fatal Error");
            //if (e.Exception.InnerException is not null)
            //    Logger.Error(e.Exception.InnerException.Message, "Inner Exception");
            base.OnUnhandledException(sender, e);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
#if false
            DisplayRootViewForAsync<TimerSettingsViewModel>();
            var screen = IoC.Get<TimerSettingsViewModel>();
#else
            // Show screen at startup
            DisplayRootViewForAsync<ShellViewModel>();
            var screen = IoC.Get<ShellViewModel>();
            //var view   = screen.GetView() as ShellView;
            var configuration = IoC.Get<Configuration>();

            configuration.LoadLanguageSetting(); // Load settings before opening the ControlView to ensure that the correct language is set.

            screen.OpenControlView();
#endif
            // Restore Taskbar Icon.
            Application.MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/DBF_Tools.ico", UriKind.Absolute));
        }
    }
}
