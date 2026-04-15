using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using Configuration = DBF.DataModel.Configuration;

namespace DBF.UserControls;

/// <summary>
/// Interaction logic for BridgeTimerControl.xaml
/// </summary>
public partial class BridgeTimerControl : UserControl
{
    public BridgeTimerControl()
    {
        InitializeComponent();

        if (this.IsInDesignMode())
        {
            Visibility = Visibility.Visible;

            Timer = new BridgeTimer
                    {
                        Visibility       = Visibility.Visible
                      , Foreground       = System.Windows.Media.Brushes.Black
                      , Background       = System.Windows.Media.Brushes.Orange
                      , Time             = "21:17"
                      , RoundText        = "3. Runde"
                      , Info             = "Vi spiller 7 runder af 24 spil"
                                         + Environment.NewLine +
                                         //, MoreInfo         = 
                                         "Pause efter 4. Runde"
                      , MinutesLeft      = 13d
                      , WarningVisiblity = Visibility.Visible
                    };

            Configuration = new Configuration() { StartDate = DateTime.Now };
        }
    }

    public Configuration Configuration { get => field ??= IoC.Get<Configuration>(); set => field = value; }

    #region Dependency Properties
        #region Timer Dependency Property
            public static readonly DependencyProperty BridgeTimerProperty = 
                                   DependencyProperty.Register( nameof(Timer)
                                                              , typeof(BridgeTimer)
                                                              , typeof(BridgeTimerControl)
                                                              , new FrameworkPropertyMetadata(null,onBridgeTimerPropertyChanged));
            public BridgeTimer Timer
            {
                get => (BridgeTimer)GetValue(BridgeTimerProperty);
                set => SetValue(BridgeTimerProperty, value);
            }

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
                                                              , new FrameworkPropertyMetadata(Visibility.Visible));//, onButtonsVisibilityPropertyChanged));

            //private static void onButtonsVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            //{
            //    //if (d is BridgeTimerControl ctl)
            //    //    // ctl.CanClose =
            //    //ctl.ButtonsVisibility == Visibility.Visible;
            //}
        #endregion
    #endregion

    #region Public Properties
        public Visibility UpButtonVisibility   { get; private set; }
        public Visibility DownButtonVisibility { get; private set; }
    #endregion

    #region Private Methods        
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.BackAll();
            else

                Timer.Back();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Configuration.CloseTimer(Timer);

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.ForwardAll();
            else

                Timer.Forward();
        }

        private void BtnLessTime_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.LessTimeAll();
            else

                Timer.LessTime();
        }

        private void BtnMoreTime_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.MoreTimeAll();
            else
                Timer.MoreTime();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.PauseAll();
            else

                Timer.Pause();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.ResetAll();
            else

                Timer.Reset();
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e) => Timer.OpenSetting();

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.StartAll();
            else
                Timer.Start();
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)   => Configuration.TimerUp(Timer);

        private void BtnDown_Click(object sender, RoutedEventArgs e) => Configuration.TimerDown(Timer);
    #endregion
}
