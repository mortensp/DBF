using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.Json.Serialization;
using System.Transactions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.Helpers;
using DBF.ViewModels;
using NAudio.MediaFoundation;
using Syncfusion.UI.Xaml.Schedule;

namespace DBF.DataModel
{
    public partial class BridgeTimer : TimerSetting
    {
        private IAudioService _player { get => field ??= IoC.Get<IAudioService>(); set => field = value; }
        //
        private                 DispatcherTimer _timer;
        private static readonly TimeSpan        _oneHour        = new TimeSpan(1, 0,  0);
        private static readonly TimeSpan        _oneMinute      = new TimeSpan(0, 1,  0);
        private static readonly TimeSpan        _twoMinutes     = new TimeSpan(0, 2,  0);
        private static readonly TimeSpan        _threshold      = new TimeSpan(0, 0,  0);
        private                 TimeSpan        _startTime      = new TimeSpan(                   0, 21, 0);
        private                 TimeSpan        _transitionTime = new TimeSpan(                   0, 1,  0);
        private                 TimeSpan        _breakTime      = new TimeSpan(                   0, 12, 0);
        private                 TimeSpan        _warningTime    = new TimeSpan(                   0, 5,  0);
        private                 bool            _isAtBreak;
        private                 bool            _isAtTransition;
        private Preset BMSettings { get => field; set => field = value; }
        //
        private readonly object _sync = new object();

        private TimeSpan _remainingTime
        {
            get => field;
            set
            {
                Set(ref field, value);
            }
        } = TimeSpan.MinValue;

        public BridgeTimer()
        {
            _timer                              = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick                        += Timer_Tick;
            ChangedProperties.CollectionChanged+= ChangedProperties_CollectionChanged;
        }

        #region Public Properties
            [JsonIgnore] public TimeSpan RemainingTime => _remainingTime;
            [JsonIgnore] public bool IsStarted    => Round >  0;
            [JsonIgnore] public bool IsActive     => Round >  0 && Round <= Rounds;
            [JsonIgnore] public bool IsEnded      => Round >  Rounds;
            [JsonIgnore] public bool IsRunning    => _timer.IsEnabled;

            [JsonIgnore] public Configuration Configuration    { get => field ??= IoC.Get<Configuration>(); set => field = value; }
            [JsonIgnore] public string        Time             { get; set; }
            [JsonIgnore] public bool          CanClose         { get; set; }
            [JsonIgnore] public Visibility    WarningVisiblity { get; set; }
            [JsonIgnore] public string        RoundText        { get; set; }
            [JsonIgnore] public Visibility    ShowUpButton     { get; set; }
            [JsonIgnore] public Visibility    ShowDownButton   { get; set; }
            [JsonIgnore] public double        MinutesLeft      { get; set; }

            [JsonIgnore]
            public int Round
            {
                get => field;
                set => Set(ref field, Math.Max(0, value));
            }

            [JsonIgnore]
            public TimeOnly? EndTime
            {
                get
                {
                    return Configuration.StartTime?.AddMinutes(Math.Max(MinutesLeft, 0));
                }
            }

            [JsonIgnore]
            public DateTime? PauseEndTime
            {
                get
                {
                    if (Round <  1
                    ||  Round >  BreakAfterRound)
                        return null;

                    var time = DateTime.Now.AddTimeSpan(_remainingTime);

                    for (var r = Round + 1; r <= BreakAfterRound; r++)
                    {
                        time = time.AddTimeSpan(new TimeSpan(Hours, Minutes, Seconds));
                        time = time.AddMinutes(TransitionMinutes);
                    }

                    if (_isAtBreak)
                        return time;
                    else
                        return time.AddMinutes(BreakMinutes);
                }
            }

            [JsonIgnore]
            public DateTime? EndTimeSession
            {
                get
                {
                    if (IsEnded)
                        return null;

                    var time = PauseEndTime
                            ?? DateTime.Now.AddTimeSpan(_remainingTime);

                    int r = Round >  BreakAfterRound
                          ? Round+1
                          : BreakAfterRound +1;

                    for (; r <= Rounds; r++)

                    {
                        time = time.AddTimeSpan(new TimeSpan(Hours, Minutes, Seconds));

                        if (r <  Rounds)
                            time = time.AddMinutes(TransitionMinutes);
                    }

                    return time;
                }
            }

            [JsonIgnore]
            public BridgeTimerState CurrentState => new BridgeTimerState()
                                                    {
                                                        IsStarted = IsStarted
                                                      , IsAtBreak = _isAtBreak
                                                      , IsPaused = _timer.IsEnabled
                                                      , IsAtTransition = _isAtTransition
                                                      , RemainingTime = _remainingTime
                                                      , Round = Round
                                                    };

            [JsonIgnore]
            public string BadgeText
            {
                get => Visibility == Visibility.Visible
                   &&  ChangedProperties?.Count >  0
                     ? field
                     : string.Empty;
                private set => field = value;
            } = "Nb";
        #endregion

        #region Public Methods
            public void UpdateBMSettings(Preset preset)
            {
                BMSettings = preset;

                MarkChanged(preset);
            }

            public async void OpenSetting()
            {
                var screen        = IoC.Get<TimerSettingsViewModel>();
                var windowManager = IoC.Get<IWindowManager>();
                screen.Setting    = this;
                //screen.Message    = message;
                screen.SuggestedSettings(BMSettings);
                await windowManager.ShowDialogAsync(screen);

                // If the dialog was enden with save, then our Settings was been updated
                ChangedProperties.Clear();
                BMSettings = null;
                initTimeSettings();
                updateDisplay();
            }

            public void UpdateDisplay()
            {
                lock (_sync)
                {
                    updateDisplay();
                }
            }

            public void Close()
            {
                lock (_sync)
                {
                    var result = MessageBox.Show(Lex.CloseAndResetWarning, Lex.Confirmation, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.OK)
                    {
                        stopTimer();
                        Visibility = Visibility.Collapsed;
                        Configuration.Save();
                    }
                }
            }

            public void Start()
            {
                lock (_sync)
                {
                    if (Round <  1)
                        Round =  1;

                    if (!IsEnded)
                    {
                        if (!_timer.IsEnabled
                        && !_isAtBreak
                        && !_isAtTransition
                        &&  _remainingTime == _startTime
                        && !_isAtBreak
                        && !_isAtTransition)
                            _player.Play("Ding Ding", Volume);  // Start next round

                        //_player.Play(Sound, Volume);    // End of round
                        _timer.Start();
                    }
                }
            }

            public void Stop()
            {
                lock (_sync)
                {
                    _timer.Stop();
                }
            }

            public void Back()
            {
                lock (_sync)
                {
                    var enaled =_timer.IsEnabled;
                    _timer.Stop();

                    if (_isAtBreak)
                        if (_remainingTime == _breakTime)
                        {
                            _isAtBreak     = false;
                            _remainingTime = _startTime;
                        }
                        else
                            _remainingTime = _breakTime;
                    else
                        if (_isAtTransition)
                        {
                            _isAtTransition = false;
                            _remainingTime  = _startTime;
                        }
                        else
                            if (Round <= Rounds
                            &&  _startTime.Subtract(_remainingTime) >  _threshold)
                                _remainingTime = _startTime;
                            else
                            {
                                Round--;

                                if (Round == BreakAfterRound)
                                {
                                    _remainingTime = _breakTime;
                                    _isAtBreak     = true;
                                }
                                else
                                    _remainingTime = _startTime;
                            }

                    if (Round == 1)
                        Round =  0;

                    updateDisplay();
                }
            }

            public void Pause(bool force = false)
            {
                lock (_sync)
                {
                    if (IsStarted)
                        if (force
                        ||  _timer.IsEnabled
                        ||  IsEnded)
                            _timer.Stop();
                        else
                            Start();
                }
            }

            public void Forward()
            {
                lock (_sync)
                {
                    _timer.Stop();

                    if (Round <= Rounds)
                    {
                        if (_isAtBreak)
                        {
                            _isAtBreak = false;
                            IncremetRound();
                            _remainingTime = _startTime;
                        }
                        else
                            if (_remainingTime >  TimeSpan.Zero)
                                if (!_isAtBreak && Round == BreakAfterRound && BreakAfterRound >  0)
                                {
                                    _remainingTime = _breakTime;
                                    _isAtBreak     = true;
                                }
                                else
                                {
                                    IncremetRound();
                                    _remainingTime  = _startTime;
                                    _isAtTransition = false;
                                }
                    }

                    updateDisplay();
                }
            }

            private void IncremetRound()
            {
                Round = Math.Max(2, Round + 1);
            }

            public void LessTime()
            {
                lock (_sync)
                {
                    _remainingTime = DBFMath.Max(TimeSpan.Zero, _remainingTime.Add(TimeSpan.FromSeconds(-30)));
                    updateDisplay();
                }
            }

            public void MoreTime()
            {
                lock (_sync)
                {
                    _remainingTime = _remainingTime.Add(TimeSpan.FromSeconds(60));
                    updateDisplay();
                }
            }

            public void FinishRound(int round)
            {
                lock (_sync)
                {
                    if (round <  Round
                    ||  round == Round && (_isAtBreak || _isAtTransition))
                        return;

                    if (IsActive
                    &&  _timer.IsEnabled)
                    {
                        if (_isAtTransition)
                            Debugger.Break();

                        if (_isAtBreak)
                            Debugger.Break();

                        _remainingTime = TimeSpan.Zero;
                        Round          = round;
                        updateDisplay();
                    }
                    else
                    {
                        Round = round;
                        Forward();
                    }
                }
            }

            public void SetRound(int round)
            {
                lock (_sync)
                {
                    Round = round;

                    //if (Round == BMRounds && !TeamMatch)
                    //    _remainingTime += _transitionTime;
                    updateDisplay();
                }
            }

            public void Reset(bool ask = true, bool plural = false)
            {
                lock (_sync)
                {
                    if (ask == false
                    ||  IsStarted           == false
                    ||  MessageBoxResult.OK == MessageBox.Show( plural
                                                              ? $"{Lex.ResetTimersPlural} {Lex.ContinueQuestion}"
                                                              : $"{Lex.ResetTimers} {Lex.ContinueQuestion}"
                                                              , Lex.Confirmation
                                                              , MessageBoxButton.OKCancel
                                                              , MessageBoxImage.Question))
                    {
                        stopTimer();
                        _isAtBreak      = false;
                        _isAtTransition = false;
                        Round           = 0;
                        initTimeSettings();

                        updateDisplay();
                    }
                }
            }

            public void Restore(BridgeTimerState state)
            {
                lock (_sync)
                {
                    //stopTimer();
                    _isAtBreak      = state.IsAtBreak;
                    _isAtTransition = state.IsAtTransition;
                    _remainingTime  = state.RemainingTime;
                    Round           = state.Round;
                    //
                    _startTime      = new TimeSpan(Hours, Minutes, Seconds);
                    _transitionTime = new TimeSpan(0, TransitionMinutes, 0);
                    _breakTime      = new TimeSpan(0, BreakMinutes, 0);

                    if (state.IsPaused && state.IsStarted)
                        Start();
                    else
                        stopTimer();

                    updateDisplay();
                }
            }
        #endregion

        #region Private Methods
            private void stopTimer()
            {
                _timer.Stop();
                updateDisplay();
            }

            private void updateDisplay()
            {
                if (_timer.IsEnabled)
                    if (!_isAtBreak
                    &&  _warningTime == _remainingTime
                    &&  _warningTime >  TimeSpan.Zero
                    &&  _warningTime <  _startTime)
                        _player.Play(Sound, Volume);                            // Warning before end of round
                    else
                        if (_isAtBreak
                        &&  _startTime     >  _twoMinutes
                        &&  _remainingTime == _twoMinutes)
                            _player.Play(Sound, Volume);                        // Warning two minutes before end of Break
                        else
                            if (_remainingTime <= TimeSpan.Zero)
                                if (!_isAtBreak
                                &&  Round           == BreakAfterRound
                                &&  BreakAfterRound >  0)
                                {
                                    _player.Play("Ding Ding", Volume);          // Break
                                    _remainingTime  = _breakTime;
                                    _isAtTransition = false;
                                    _isAtBreak      = true;
                                }
                                else
                                    if (Round >= Rounds)
                                    {
                                        _player.Play(Sound, Volume);            // End of game                                        
                                        _timer.Stop();
                                        Round = Rounds + 1;
                                    }
                                    else
                                        if (_isAtTransition
                                        ||  _isAtBreak)
                                        {
                                            _player.Play("Ding Ding", Volume);  // Start next round
                                            _remainingTime  = _startTime;
                                            _isAtTransition = false;
                                            _isAtBreak      = false;
                                            IncremetRound();
                                        }
                                        else
                                            if (_transitionTime >  TimeSpan.Zero)
                                            {
                                                _player.Play(Sound, Volume);    // FinishRound
                                                _isAtTransition = true;
                                                _remainingTime  = _transitionTime;
                                            }
                                            else
                                            {
                                                _player.Play(Sound, Volume);    // End of round
                                                _remainingTime = _startTime;
                                                IncremetRound();
                                            }

                if (_remainingTime == TimeSpan.MinValue)
                    initTimeSettings();

                Time = _remainingTime.ToString(_remainingTime <  _oneHour ? @"mm\:ss" : @"hh\:mm\:ss");

                if (Round          >  Rounds
                ||  _remainingTime <= TimeSpan.Zero)
                    WarningVisiblity = Visibility.Collapsed;
                else
                    if (_isAtBreak)
                        WarningVisiblity = ( _remainingTime <= _twoMinutes
                                         &&  _breakTime >  _twoMinutes)
                                         ? Visibility.Visible
                                         : Visibility.Collapsed;
                    else
                        WarningVisiblity = ( _remainingTime <= _warningTime
                                         &&  _startTime >  _warningTime)
                                         ? Visibility.Visible : 
                                         Visibility.Collapsed;

                Info = $"{Lex.WeArePlaying} {Rounds} {getRoundsText} {Lex.Of} {BoardsPerRound} {Lex.Boards}";

                if (Round == Rounds && !TeamMatch)
                {
                    RoundText = Lex.LastRound;
                    Info      = string.Empty;
                }
                else
                    if (Round <= Rounds)
                        if (_isAtBreak)
                        {
                            RoundText = $"{Lex.PauseUntil}: " + PauseEndTime?.ToString("HH:mm");
                            Info      = PauseMessage;
                        }
                        else
                        {
                            if (Round           <= BreakAfterRound
                            &&  BreakAfterRound >  0
                            &&  BreakMinutes    >  0)
                                Info += Environment.NewLine
                                      + $"{BreakMinutes} {Lex.MinutesBreakAfter} {BreakAfterRound}. {getRoundText}";

                            if (_isAtTransition)
                                RoundText = $"{Lex.TransitioningTo} {Math.Max(2, Round + 1)}. {getRoundText}";
                            else
                                RoundText = $"{Math.Max(1, Round)}. {getRoundText}";
                        }
                    else
                    {
                        RoundText = EndGreetingTop
                                 ?? Lex.ThanksForNow;
                        Info      = EndGreetingBottom
                                 ?? Lex.RememberToCleanUp;
                        Time      = string.Empty;
                    }

                var remainingRounds = Rounds - Round;
                var left            = _remainingTime.TotalSeconds / 60d + remainingRounds * (Hours * 60 + Minutes + Seconds / 60d);

                if (Round <= BreakAfterRound && !_isAtBreak)
                    left += BreakMinutes;

                if (!_isAtTransition)
                    if (Round <  BreakAfterRound)
                        left += (remainingRounds - 1) * TransitionMinutes;
                    else
                        left += remainingRounds * TransitionMinutes;

                MinutesLeft = Math.Ceiling(left); // rundet op
            }

            private void initTimeSettings()
            {
                _startTime      = new TimeSpan(Hours, Minutes, Seconds);
                _breakTime      = new TimeSpan(0, BreakMinutes, 0);
                _transitionTime = new TimeSpan(0, TransitionMinutes, 0);
                _warningTime    = new TimeSpan(0, WarningMinutes, 0);

                if (!IsStarted)
                    _remainingTime = _startTime;
            }

            private void ChangedProperties_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyOfPropertyChange(nameof(BadgeText));
            }

            #region Private Timer Events         
                private void Timer_Tick(object sender, EventArgs e)
                {
                    if (_timer.IsEnabled)
                    {
                        _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                        updateDisplay();
                    }
                }

                public void UpdateBMSettings(bool teams, int rounds, int boardsPerRound)
                {
                    if (Rounds         != rounds
                    ||  BoardsPerRound != boardsPerRound)
                    {
                        var preset = Configuration.Presets
                                                  .FirstOrDefault(p=>  p.TeamMatch      ==teams
                                                                   &&  p.Rounds         == rounds
                                                                   &&  p.BoardsPerRound == boardsPerRound);

                        if (preset != null)
                            Logger.Info($"Timer settings syncronized with BridgeMate Server - Preset: {preset.Name}");
                        else
                        {
                            preset                   = new(this);
                            preset.Name              = null;
                            preset.Rounds            = rounds;
                            preset.BoardsPerRound    = boardsPerRound;
                            preset.Minutes           = boardsPerRound * 7;
                            preset.TransitionMinutes = 0;
                            preset.BreakAfterRound   = (rounds + 1) / 2;
                        }

                        UpdateBMSettings(preset);
                    }
                }

                private string getRoundText  => TeamMatch ? Lex.Half : Lex.Round;
                private string getRoundsText => TeamMatch ? Lex.Halfs : Lex.Rounds;
            #endregion
        #endregion
    }
}
