using Caliburn.Micro;
using DBF.DataModel;
using DBF.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for BridgeTimerControlOld.xaml
    /// </summary>
    public partial class BridgeTimerControlOld : UserControl , INotifyPropertyChanged
    {
        private readonly        IWindowManager  _windowManager;
        private static readonly TimeSpan        _oneHour        = new TimeSpan(1, 0, 0);
        private static readonly TimeSpan        _fiveMinutes    = new TimeSpan(0, 5, 0);
        private static readonly TimeSpan        _twoMinutes     = new TimeSpan(0, 2, 0);
        private                 TimeSpan        _startTime      = new TimeSpan(0, 21, 0);
        private                 TimeSpan        _transitionTime = new TimeSpan(0, 1, 0);
        private                 TimeSpan        _warningTime    = new TimeSpan(0, 1, 0);
        private                 TimeSpan        _breakTime      = new TimeSpan(0, 12, 0);
        private                 TimeSpan        _remainingTime  = new TimeSpan(0, 21, 0);
        private                 DispatcherTimer _timer;
        private                 bool            _isStarted;
        private                 bool            _isAtBreak      = false;
        private                 bool            _isAtTransition = false;
        private                 bool            _isPaused;
        private                 int             _round          = 1;

        public Configuration Configuration   { get; private set; }

        public BridgeTimerControlOld()
        {
            InitializeComponent();

            _windowManager  = IoC.Get<IWindowManager>();
            Configuration   = IoC.Get<Configuration>();
            _timer          = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick    += Timer_Tick;
        }

        #region Dependency Properties
            #region Time Dependency Property
                public string Time
                {
                    get => (string)GetValue(TimeProperty);
                    set => SetValue(TimeProperty, value);
                }

                public static readonly DependencyProperty TimeProperty = DependencyProperty.Register( nameof(Time)
                                                                                                    , typeof(string)
                                                                                                    , typeof(BridgeTimerControlOld)
                                                                                                    , new FrameworkPropertyMetadata("21:00"));
            #endregion

            #region WarningVisiblity Dependency Property
                public Visibility WarningVisiblity
                {
                    get => (Visibility)GetValue(WarningVisiblityProperty);
                    set => SetValue(WarningVisiblityProperty, value);
                }

                public static readonly DependencyProperty WarningVisiblityProperty = DependencyProperty.Register( nameof(WarningVisiblity)
                                                                                                                , typeof(Visibility)
                                                                                                                , typeof(BridgeTimerControlOld)
                                                                                                                , new FrameworkPropertyMetadata(Visibility.Hidden));

                #region Setting Dependency Property
                    public TimerSetting Setting
                    {
                        get => (TimerSetting)GetValue(SettingProperty);
                        set => SetValue(SettingProperty, value);
                    }

                    public static readonly DependencyProperty SettingProperty = DependencyProperty.Register( nameof(Setting)
                                                                                                           , typeof(TimerSetting)
                                                                                                           , typeof(BridgeTimerControlOld)
                                                                                                           , new FrameworkPropertyMetadata(null, onSettingPropertyChanged));

                    // Tilføj dette felt til BridgeTimerControlOld-klassen:
                    private PropertyChangedEventHandler _settingChangedHandler;

                    private static void onSettingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                    {
                        if (d is BridgeTimerControlOld ctl)
                        {
                            ctl._settingChangedHandler = (s, args) => onSettingChanged(s, args, ctl);

                            if (e.OldValue is TimerSetting oldSetting)
                                oldSetting.PropertyChanged -= ctl._settingChangedHandler;

                            if (e.NewValue is TimerSetting newSetting)
                            {
                                newSetting.PropertyChanged += ctl._settingChangedHandler;

                                var setting         = ctl.Setting;
                                ctl.BackgroundBrush = new SolidColorBrush(setting.Color ?? Colors.White);

                                ctl.Visibility = setting.Visibility;

                                if (setting.Visibility != Visibility.Visible)
                                {
                                    ctl._isStarted = false;
                                    ctl.StopCountdown();
                                }

                                ctl._startTime      = new TimeSpan(setting.Hours, setting.Minutes, setting.Seconds);
                                ctl._transitionTime = new TimeSpan(0, setting.TransitionTime, 0);
                                ctl._breakTime      = new TimeSpan(0, setting.BreakTime, 0);
                                ctl._warningTime    = _fiveMinutes.Subtract(ctl._transitionTime);

                                if (ctl._warningTime >= ctl._startTime)
                                    ctl._warningTime =  TimeSpan.Zero;

                                if (!ctl.IsStarted)
                                {
                                    ctl._round         = 1;
                                    ctl._remainingTime = ctl._startTime;
                                }

                                ctl.Info = $"Vi spiller {setting.Rounds} {RoundsText(setting)} af {setting.BoardsPerRound} spil";
                                ctl.updateTextFields();
                            }
                        }
                    }

                    private static void onSettingChanged(object sender, PropertyChangedEventArgs e, BridgeTimerControlOld ctl)
                    {
                        if (sender         is TimerSetting setting
                        &&  e.PropertyName == nameof(TimerSetting.Visibility))
                            ctl.Visibility = setting.Visibility;
                    }
                #endregion

                #region Group Dependency Property
                    public string Group
                    {
                        get => (string)GetValue(GroupProperty);
                        set => SetValue(GroupProperty, value);
                    }

                    public static readonly DependencyProperty GroupProperty = DependencyProperty.Register( nameof(Group)
                                                                                                         , typeof(string)
                                                                                                         , typeof(BridgeTimerControlOld)
                                                                                                         , new FrameworkPropertyMetadata("A og C", onGroupPropertyChanged));

                    private static void onGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                    {
                    }
                #endregion

                #region Round Dependency Property
                    public string Round
                    {
                        get => (string)GetValue(RoundProperty);
                        set => SetValue(RoundProperty, value);
                    }

                    public static readonly DependencyProperty RoundProperty = DependencyProperty.Register( nameof(Round)
                                                                                                         , typeof(string)
                                                                                                         , typeof(BridgeTimerControlOld)
                                                                                                         , new FrameworkPropertyMetadata("1. Runde/Halvleg", onRoundPropertyChanged));

                    private static void onRoundPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                    {
                    }
                #endregion

                #region Info Dependency Property
                    public string Info
                    {
                        get => (string)GetValue(InfoProperty);
                        set => SetValue(InfoProperty, value);
                    }

                    public static readonly DependencyProperty InfoProperty = DependencyProperty.Register( nameof(Info)
                                                                                                        , typeof(string)
                                                                                                        , typeof(BridgeTimerControlOld)
                                                                                                        , new FrameworkPropertyMetadata("Info", onInfoPropertyChanged));

                    private static void onInfoPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                    {
                    }
                #endregion

                #region MoreInfo Dependency Property
                    public string MoreInfo
                    {
                        get => (string)GetValue(MoreInfoProperty);
                        set => SetValue(MoreInfoProperty, value);
                    }

                    public static readonly DependencyProperty MoreInfoProperty = DependencyProperty.Register( nameof(MoreInfo)
                                                                                                            , typeof(string)
                                                                                                            , typeof(BridgeTimerControlOld)
                                                                                                            , new FrameworkPropertyMetadata("More Info", onMoreInfoPropertyChanged));

        public event PropertyChangedEventHandler PropertyChanged;

        private static void onMoreInfoPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
                    {
                    }
                #endregion
            #endregion
        #endregion

        #region Public Methods
            public Brush         BackgroundBrush { get; set; }
            public bool  IsPaused  => _isPaused;
            public bool  IsStarted => _isStarted;
        #endregion

        #region Public Button Click Events
            public async void BtnSetting_Click(object sender, RoutedEventArgs e)
            {
                var screen     = IoC.Get<TimerSettingViewModel>();
                screen.Setting = Setting;

                await _windowManager.ShowDialogAsync(screen);

                // If the dialog was enden with save, then our Settings was updated
                _startTime      = new TimeSpan(Setting.Hours, Setting.Minutes, Setting.Seconds);
                _breakTime      = new TimeSpan(0, Setting.BreakTime, 0);
                _transitionTime = new TimeSpan(0, Setting.TransitionTime, 0);

                if (!IsStarted)
                    _remainingTime = _startTime;

                BackgroundBrush = new SolidColorBrush(Setting.Color ?? Colors.White);

                updateTextFields();
            }

            public void btnClose_Click(object sender, RoutedEventArgs e)
            {
                var result = MessageBox.Show("Hvis du lukker uret, så nulstilles det. Vil du lukke uret?", "Bekræftelse", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    this.StopCountdown();
                    this.Visibility = Setting.Visibility = Visibility.Collapsed;
                    Configuration.Save();
                }
            }

            public void BtnStart_Click(object sender, RoutedEventArgs e)
            {
                if (!_isStarted || _isPaused)
                {
                    _isStarted = true;
                    _isPaused  = false;
                    StartCountdown();
                }
            }

            public void BtnBack_Click(object sender, RoutedEventArgs e)
            {
                StopCountdown();

                if (_round == 1)
                {
                    _isStarted     = false;
                    _remainingTime = _startTime;
                }
                else
                    if (_isAtBreak)
                        if (_remainingTime == _breakTime)
                        {
                            _isAtBreak     = false;
                            _remainingTime = _startTime;
                        }
                        else
                            _remainingTime = _breakTime;
                    else
                        if (_isAtTransition || _remainingTime <  _startTime)
                        {
                            _isAtTransition = false;
                            _remainingTime  = _startTime;
                        }
                        else
                            if (_remainingTime <  _startTime)
                                _remainingTime =  _startTime;
                            else
                            {
                                _round--;

                                if (_round == Setting.BreakAfter)
                                {
                                    _remainingTime = _breakTime;
                                    _isAtBreak     = true;
                                }
                                else
                                    _remainingTime = _startTime;
                            }

                updateTextFields();
            }

            public void BtnPause_Click(object sender, RoutedEventArgs e)
            {
                if (_isStarted)
                    if (_isPaused)
                    {
                        _isPaused = false;
                        _timer.Start();
                    }
                    else
                    {
                        _isPaused = true;
                        _timer.Stop();
                    }
            }

            public void BtnForward_Click(object sender, RoutedEventArgs e)
            {
                StopCountdown();

                if (_round <= Setting.Rounds)
                {
                    if (_isAtBreak)
                    {
                        _isAtBreak = false;
                        _round++;
                        _remainingTime = _startTime;
                    }
                    else
                        if (_remainingTime >  TimeSpan.Zero)
                            if (!_isAtBreak && _round == Setting.BreakAfter)
                            {
                                _remainingTime = _breakTime;
                                _isAtBreak     = true;
                            }
                            else
                            {
                                _round++;
                                _remainingTime = _startTime;
                            }
                }

                updateTextFields();
            }

            public void BtnLessTime_Click(object sender, RoutedEventArgs e)
            {
                _remainingTime = DBFMath.Max(TimeSpan.Zero, _remainingTime.Add(TimeSpan.FromSeconds(-30)));
                updateTextFields();
            }

            public void BtnMoreTime_Click(object sender, RoutedEventArgs e)
            {
                _remainingTime = _remainingTime.Add(TimeSpan.FromSeconds(60));
                updateTextFields();
            }

            public void BtnReset_Click(object sender, RoutedEventArgs e)
            {
                var result = MessageBox.Show("Dette nulstiller uret fuldtsændigt. Vil du nulstille uret?", "Bekræftelse", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    var setting = Setting;
                    StopCountdown();
                    _isAtBreak      = false;
                    _isAtTransition = false;
                    _isPaused       = true;
                    _isStarted      = false;
                    _isStarted      = false;
                    _remainingTime  = _startTime;
                    _startTime      = new TimeSpan(setting.Hours, setting.Minutes, setting.Seconds);
                    _transitionTime = new TimeSpan(0, setting.TransitionTime, 0);
                    _breakTime      = new TimeSpan(0, setting.BreakTime, 0);
                    _round          = 1;

                    Info = $"Vi spiller {setting.Rounds} {RoundsText(setting)} af {setting.BoardsPerRound} spil";
                    updateTextFields();
                }
            }
        #endregion

        #region Private Timer Events
            private void StartCountdown()
            {
                _isPaused = false;

                _timer.Start();
            }

            private void PauseCountdown()
            {
                _isPaused = true;
            }

            private void StopCountdown()
            {
                _timer.Stop();
                _isPaused = true;
                //_remainingTime = _startTime;
                updateTextFields();
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                if (!_isPaused && _remainingTime.TotalSeconds >  0)
                {
                    _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                    updateTextFields();
                }
                else
                    if (_remainingTime.TotalSeconds <= 0)
                        _timer.Stop();
            }

            private void updateTextFields()
            {
                if (!_isPaused)
                    if (_remainingTime == _warningTime)
                        AudioPlayer.Play(Setting.Sound, Setting.Volume);                            // Warning in Round
                    else
                        if (_isAtBreak && _remainingTime == _twoMinutes)
                            AudioPlayer.Play(Setting.Sound, Setting.Volume);                        // Warning at Break
                        else
                            if (_remainingTime == TimeSpan.Zero)
                                if (!_isAtBreak && _round == Setting.BreakAfter)
                                {
                                    AudioPlayer.Play(Setting.Sound, Setting.Volume);                // Break
                                    _remainingTime  = _breakTime;
                                    _isAtTransition = false;
                                    _isAtBreak      = true;
                                }
                                else
                                    if (_round >= Setting.Rounds)
                                    {
                                        AudioPlayer.Play(Setting.Sound, Setting.Volume);            // End of game                                        
                                        _round++;
                                        _timer.Stop();
                                    }
                                    else
                                        if (_isAtTransition || _isAtBreak)
                                        {
                                            AudioPlayer.Play("Ding Ding", Setting.Volume);          // Start next round
                                            _remainingTime  = _startTime;
                                            _isAtTransition = false;
                                            _isAtBreak      = false;
                                            _round++;
                                        }
                                        else
                                            if (_transitionTime == TimeSpan.Zero)
                                            {
                                                AudioPlayer.Play(Setting.Sound, Setting.Volume);    // End of round
                                                _remainingTime = _startTime;
                                                _round++;
                                            }
                                            else
                                            {
                                                AudioPlayer.Play(Setting.Sound, Setting.Volume);    // Transition
                                                _isAtTransition = true;
                                                _remainingTime  = _transitionTime;
                                            }

                Time = _remainingTime.ToString(_remainingTime <  _oneHour ? @"mm\:ss" : @"hh\:mm\:ss");

                if (_isAtBreak)
                    WarningVisiblity = (_remainingTime >  TimeSpan.Zero && _remainingTime <  _twoMinutes && _breakTime >= _twoMinutes) ? Visibility.Visible : Visibility.Collapsed;
                else
                    WarningVisiblity = (_remainingTime >  TimeSpan.Zero && _remainingTime <  _warningTime) ? Visibility.Visible : Visibility.Collapsed;

                MoreInfo = string.Empty;

                if (_round == Setting.Rounds)
                {
                    Round = $"Sidste runde!";
                    Info  = string.Empty;
                }
                else
                    if (_round <  Setting.Rounds)
                        if (_isAtBreak)
                            Round = $"Pause!";
                        else
                        {
                            if (_round <= Setting.BreakAfter)
                                MoreInfo = $"Pause efter {Setting.BreakAfter}. {RoundText(Setting)}";

                            if (_isAtTransition)
                                Round = $"Der skiftes til {_round}. {RoundText(Setting)}";
                            else
                                Round = $"{_round}. {RoundText(Setting)}";
                        }
                    else
                    {
                        Round    = $"Tak for i god ro og orden.";
                        Info     = "Husk at aflevere kort, melde-";
                        MoreInfo = "kasser mm. ovre på reolen";
                        Time     = string.Empty;
                    }
            }

            private static string RoundText(TimerSetting setting)  => setting.TeamMatch ? "halvleg" : "runde";

            private static string RoundsText(TimerSetting setting) => setting.TeamMatch ? "halvlege" : "runder";

            private void updateAll()
            {
                Time = _remainingTime <  _oneHour
                     ? _remainingTime.ToString(@"mm\:ss")
                     : _remainingTime.ToString(@"mm\:ss");
            }
        #endregion
    }
}
