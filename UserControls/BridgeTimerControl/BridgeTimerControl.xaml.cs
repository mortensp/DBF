using System.ComponentModel;
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

        public DBF.DataModel.Configuration Configuration        { get => configuration ?? (configuration = IoC.Get<DBF.DataModel.Configuration>()); private set => configuration = value; }

        #region Dependency Properties
            #region Timer Dependency Property
                public BridgeTimer Timer
                {
                    get=> (BridgeTimer)GetValue(BridgeTimerProperty);
                    set=> SetValue(BridgeTimerProperty, value);
                }

                public static readonly DependencyProperty BridgeTimerProperty = DependencyProperty.Register( nameof(Timer)
                                                                                                           , typeof(BridgeTimer)
                                                                                                           , typeof(BridgeTimerControl)
                                                                                                           , new FrameworkPropertyMetadata(null, onBridgeTimerPropertyChanged));

                private static void onBridgeTimerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                {
                    if (d is BridgeTimerControl ctl)
                    {
                        if (e.OldValue is BridgeTimer oldValue)
                            oldValue.PropertyChanged -= ctl.Timer_PropertyChanged;

                        if (e.NewValue is BridgeTimer newValue)
                        {
                            newValue.PropertyChanged+= ctl.Timer_PropertyChanged;
                            ctl.UpdateText(ctl.Timer);
                        }
                    }
                }

                private void Timer_PropertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (sender         is BridgeTimer timer
                    &&  e.PropertyName == nameof(Timer.ForegroundColor))
                        UpdateText(timer);
                }

                private void UpdateText(BridgeTimer timer)
                {
                    foreach (var elm in this.display.Children.OfType<TextBlock>())
                        elm.Foreground = timer.Foreground;
                }
            #endregion

            #region CanClose Dependency Property
                public bool CanClose
                {
                    get=> (bool)GetValue(CanCloseProperty);
                    set=> SetValue(CanCloseProperty, value);
                }

                public static readonly DependencyProperty CanCloseProperty = DependencyProperty.Register( nameof(CanClose)
                                                                                                        , typeof(bool)
                                                                                                        , typeof(BridgeTimerControl)
                                                                                                        , new FrameworkPropertyMetadata(true));
            #endregion

            #region Dependency Property ButtonsVisibility
                public Visibility ButtonsVisibility
                {
                    get=> (Visibility)GetValue(ButtonsVisibilityProperty);
                    set=> SetValue(ButtonsVisibilityProperty, value);
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

        #region Public Properties
            public Visibility                  UpButtonVisibility   { get; private set; }
            public Visibility                  DownButtonVisibility { get; private set; }
        #endregion

        #region Private Methods        
            private void BtnBack_Click(object sender, RoutedEventArgs e)    => Timer.Back();

            private void btnClose_Click(object sender, RoutedEventArgs e)   => Configuration.CloseTimer(Timer);

            private void BtnForward_Click(object sender, RoutedEventArgs e) => Timer.Forward();

            private void BtnLessTime_Click(object sender, RoutedEventArgs e)=> Timer.LessTime();

            private void BtnMoreTime_Click(object sender, RoutedEventArgs e)=> Timer.MoreTime();

            private void BtnPause_Click(object sender, RoutedEventArgs e)   => Timer.Pause();

            private void BtnReset_Click(object sender, RoutedEventArgs e)   => Timer.Reset();

            private void BtnSetting_Click(object sender, RoutedEventArgs e) => Timer.OpenSetting();

            private void BtnStart_Click(object sender, RoutedEventArgs e)   => Timer.Start();

            private void BtnUp_Click(object sender, RoutedEventArgs e)      => Configuration.TimerUp(Timer);

            private void BtnDown_Click(object sender, RoutedEventArgs e)    => Configuration.TimerDown(Timer);
        #endregion
    }
}
