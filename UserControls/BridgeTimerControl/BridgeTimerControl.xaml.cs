using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using DBF.DataModel;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for BridgeTimerControl.xaml
    /// </summary>
    public partial class BridgeTimerControl : UserControl
    {
        public BridgeTimerControl()
        {
            InitializeComponent();
        }

        private DBF.DataModel.Configuration configuration;
        public DBF.DataModel.Configuration  Configuration { get => configuration ?? (configuration = IoC.Get<DBF.DataModel.Configuration>()); private set => configuration = value; }

        #region Dependency Properties
            #region Timer Dependency Property
                public BridgeTimer Timer
                {
                    get => (BridgeTimer)GetValue(BridgeTimerProperty);
                    set => SetValue(BridgeTimerProperty, value);
                }

                public static readonly DependencyProperty BridgeTimerProperty = DependencyProperty.Register( nameof(Timer)
                                                                                                           , typeof(BridgeTimer)
                                                                                                           , typeof(BridgeTimerControl));
            #endregion

            #region CanClose Dependency Property
                public bool CanClose
                {
                    get => (bool)GetValue(CanCloseProperty);
                    set => SetValue(CanCloseProperty, value);
                }

                public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register( nameof(CanClose)
                                                                                                        , typeof(bool)
                                                                                                        , typeof(BridgeTimerControl)
                                                                                                        , new FrameworkPropertyMetadata(true));
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
                                                                  , typeof(BridgeTimerControl)
                                                                  , new FrameworkPropertyMetadata(Visibility.Visible, onButtonsVisibilityPropertyChanged));

                private static void onButtonsVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                    if (d is BridgeTimerControl ctl)
                        ctl.CanClose = ctl.ButtonsVisibility == Visibility.Visible;
                }
            #endregion
        #endregion

        #region Private Methods        
            private void BtnBack_Click(object sender, RoutedEventArgs e)     => Timer.Back();

            private void btnClose_Click(object sender, RoutedEventArgs e)    => Configuration.CloseTimer(Timer);

            private void BtnForward_Click(object sender, RoutedEventArgs e)  => Timer.Forward();

            private void BtnLessTime_Click(object sender, RoutedEventArgs e) => Timer.LessTime();

            private void BtnMoreTime_Click(object sender, RoutedEventArgs e) => Timer.MoreTime();

            private void BtnPause_Click(object sender, RoutedEventArgs e)    => Timer.Pause();

            private void BtnReset_Click(object sender, RoutedEventArgs e)    => Timer.Reset();

            private void BtnSetting_Click(object sender, RoutedEventArgs e)  => Timer.OpenSetting();

            private void BtnStart_Click(object sender, RoutedEventArgs e)    => Timer.Start();
        #endregion
    }
}
