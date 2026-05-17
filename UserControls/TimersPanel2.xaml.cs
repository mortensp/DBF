using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;

namespace DBF.UserControls
{
    public partial class TimersPanel : UserControl
    {
        public TimersPanel()
        {
            InitializeComponent();

   }

        public TimersPanel(Visibility buttonsVisibility)
        {
            InitializeComponent();

            ButtonsVisibility = buttonsVisibility;
        }

        #region Dependency Properties
            #region Dependency Property BridgeTimersProperty
                public ObservableCollection<BridgeTimer> BridgeTimers
                {
                    get => (ObservableCollection<BridgeTimer>)GetValue(BridgeTimersProperty);
                    set => SetValue(BridgeTimersProperty, value);
                }

                public static readonly DependencyProperty BridgeTimersProperty = 
                                       DependencyProperty.Register( nameof(BridgeTimers)
                                                                  , typeof(ObservableCollection<BridgeTimer>)
                                                                  , typeof(TimersPanel)
                                                                  , new PropertyMetadata(new ObservableCollection<BridgeTimer>()));
            #endregion

            #region Dependency Property ButtonsVisibility
                public Visibility ButtonsVisibility
                {
                    get => (Visibility)GetValue(ButtonsVisibilityProperty);
                    set => SetValue(ButtonsVisibilityProperty, value);
                }

                public static readonly DependencyProperty ButtonsVisibilityProperty = 
                                       DependencyProperty.Register( nameof(ButtonsVisibility)
                                                                  , typeof(Visibility)
                                                                  , typeof(TimersPanel)
                                                                  , new PropertyMetadata(Visibility.Visible));
            #endregion

            #region Dependency Property TimersCanBeAddedProperty
                public bool TimersCanBeAdded
                {
                    get => (bool)GetValue(TimersCanBeAddedProperty);
                    set => SetValue(TimersCanBeAddedProperty, value);
                }

                public static readonly DependencyProperty TimersCanBeAddedProperty = 
                                       DependencyProperty.Register( nameof(TimersCanBeAdded)
                                                                  , typeof(bool)
                                                                  , typeof(TimersPanel)
                                                                  , new PropertyMetadata(false));
            #endregion

             #region Dependency Property Orientation
            public Orientation Orientation
            {
                get => (Orientation)GetValue(OrientationProperty);
                set => SetValue(OrientationProperty, value);
            }

            public static readonly DependencyProperty OrientationProperty = 
                                   DependencyProperty.Register( nameof(Orientation)
                                                              , typeof(Orientation)
                                                              , typeof(TimersPanel)
                                                              , new PropertyMetadata(Orientation.Horizontal));
        #endregion
        #endregion

        private void userControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Design.IsInDesignMode())
            {
                Visibility    = Visibility.Visible;
                Configuration = new Configuration() { StartDate = DateTime.Now };
                _             = Configuration.LoadAsync();
            }
        }

        public Configuration Configuration { get => field ?? IoC.Get<Configuration>(); private set => field = value; }
    }
}
