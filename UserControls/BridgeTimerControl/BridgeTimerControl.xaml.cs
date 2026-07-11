using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using Syncfusion.Windows.Controls.Notification;

using Configuration = DBF.DataModel.Configuration;
using DragDropEffects = System.Windows.DragDropEffects;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DBF.UserControls;

/// <summary>
/// Interaction logic for BridgeTimerControl.xaml
/// </summary>
public partial class BridgeTimerControl : UserControl
{
    public BridgeTimerControl()
    {
        InitializeComponent();

        // Ensure the badge reads from this control's Timer regardless of adorner layer DataContext
        badge.SetBinding(SfBadge.ContentProperty, new Binding("BridgeTimer.BadgeText") { Source = this, Mode = BindingMode.OneWay });
    }

    public Configuration Configuration
    {
        get => field ??= IoC.Get<Configuration>();
        set => field = value;
    }

    #region Dependency Properties
    #region Timer Dependency Property
    public static readonly DependencyProperty BridgeTimerProperty =
                                   DependencyProperty.Register( nameof(BridgeTimer)
                                                              , typeof(BridgeTimer)
                                                              , typeof(BridgeTimerControl));
                                                              //, new FrameworkPropertyMetadata(null,onBridgeTimerPropertyChanged));
            public BridgeTimer BridgeTimer
            {
                get => (BridgeTimer)GetValue(BridgeTimerProperty);
                set => SetValue(BridgeTimerProperty, value);
            }

            //private static void onBridgeTimerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            //{
            //    if (d is BridgeTimerControl ctl)
            //    {
            //        if (e.OldValue is BridgeTimer oldValue)
            //            oldValue.PropertyChanged -= ctl.Timer_PropertyChanged;

            //        //if (e.NewValue is BridgeTimer newValue)
            //        //    ctl.UpdateText(ctl.BridgeTimer);
            //    }
            //}

            //private void Timer_PropertyChanged(object sender, PropertyChangedEventArgs e)
            //{
            //    if (sender         is BridgeTimer timer
            //    &&  e.PropertyName == nameof(BridgeTimer.ForegroundColor))
            //        UpdateText(timer);
            //}

            //private void UpdateText(BridgeTimer timer)
            //{
            //    //foreach (var elm in this.display.Children.OfType<TextBlock>())
            //    //    elm.Foreground = timer.Foreground;
            //}
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
        #endregion
    #endregion

    #region Public Properties
        public Visibility UpButtonVisibility   { get; private set; }
        public Visibility DownButtonVisibility { get; private set; }
    #endregion

    #region Private Methods      
        private void display_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragDrop.DoDragDrop(this, this, DragDropEffects.Move);
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.BackAll();
            else

                BridgeTimer.Back();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => Configuration.CloseTimer(BridgeTimer);

        private void BtnForward_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.ForwardAll();
            else

                BridgeTimer.Forward();
        }

        private void BtnLessTime_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.LessTimeAll();
            else

                BridgeTimer.LessTime();
        }

        private void BtnMoreTime_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.MoreTimeAll();
            else
                BridgeTimer.MoreTime();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.PauseAll();
            else

                BridgeTimer.Pause();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect alle timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.ResetAll();
            else

                BridgeTimer.Reset();
        }

        private void BtnSetting_Click(object sender, RoutedEventArgs e) => BridgeTimer.OpenSetting();

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // If Shift is held while clicking to affect all timers, otherwise just this one
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                Configuration.StartAll();
            else
                BridgeTimer.Start();
        }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            if (this.IsInDesignMode())
            {
                Visibility = Visibility.Visible;

                BridgeTimer = new BridgeTimer
                              {
                                  Visibility        = Visibility.Visible
                                , Foreground        = System.Windows.Media.Brushes.Black
                                , Background        = System.Windows.Media.Brushes.Orange
                                , Time              = "21:17"
                                , RoundText         = "3. Runde"
                                , Info              = "Vi spiller 7 runder af 24 spil"
                                                    + Environment.NewLine +
                                                    "Pause efter 4. Runde"
                                , MinutesLeft       = 13d
                                , WarningVisibility = Visibility.Visible
                              };

                Configuration = new Configuration() { StartDate = DateTime.Now };
            }
        }
    #endregion
}
