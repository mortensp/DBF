using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.Helpers;
using DBF.ViewModels;

namespace DBF.DataModel
{

    public partial class BridgeTimer : TimerSetting
    {
        private IAudioService _player { get => field ??= IoC.Get<IAudioService>(); set => field = value; }
        //
        private                 DispatcherTimer _timer;
        private static readonly TimeSpan        _oneHour    = new TimeSpan(1, 0,  0);
        private static readonly TimeSpan        _twoMinutes = new TimeSpan(0, 2,  0);
        private static readonly TimeSpan        _threshold  = new TimeSpan(0, 0,  1);
        //
        private TimeSpan _startTime      = new TimeSpan(                   0, 21, 0);
        private TimeSpan _transitionTime = new TimeSpan(                   0, 1,  0);
        private TimeSpan _breakTime      = new TimeSpan(                   0, 12, 0);
        private TimeSpan _warningTime    = new TimeSpan(                   0, 5,  0);
        private TimeSpan _remainingTime  = TimeSpan.MinValue;
        private bool     _isStarted;
        private bool     _isAtBreak;
        private bool     _isAtTransition;
        private bool     _isPaused;

        public BridgeTimer()
        {
            _timer      = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick+= Timer_Tick;
            //UpdateDisplay();
        }

        #region Public Properties
            [JsonIgnore] public Configuration Configuration    { get => field ??= IoC.Get<Configuration>(); set => field = value; }
            [JsonIgnore] public string        Time             { get; set; }
            [JsonIgnore] public Visibility    WarningVisiblity { get; set; }
            [JsonIgnore] public string        RoundText        { get; set; }
            [JsonIgnore] public Visibility    ShowUpButton     { get; set; }
            [JsonIgnore] public Visibility    ShowDownButton   { get; set; }
            [JsonIgnore] public double        MinutesLeft      { get; set; }
            [JsonIgnore] public int           Round            { get; set; } = 1;
            [JsonIgnore] public TimeOnly EndTime  => TimeOnly.FromDateTime(Configuration.StartTime.AddMinutes(Math.Max(MinutesLeft, 0)));

            [JsonIgnore]
            public bool IsPaused
            {
                get => _isPaused;
                set => Set(ref _isPaused, value);
            }

            [JsonIgnore]
            public bool IsStarted
            {
                get => _isStarted;
                set
                {
                    if (_isStarted != value)
                        _isStarted =  value;
                }
            }

            [JsonIgnore] public bool IsActive => IsStarted && Round <= Rounds && Round >  0;
            [JsonIgnore] public bool IsEnded => Round >  Rounds;
        #endregion

        #region Public Methods
            public async void OpenSetting()
            {
                var screen        = IoC.Get<TimerSettingsViewModel>();
                var windowManager = IoC.Get<IWindowManager>();
                screen.Setting    = this;
                await windowManager.ShowDialogAsync(screen);

                // If the dialog was enden with save, then our Settings was updated
                _startTime      = new TimeSpan(     Hours,  Minutes,           Seconds);
                _breakTime      = new TimeSpan(     0,      BreakMinutes,      0);
                _transitionTime = new TimeSpan(     0,      TransitionMinutes, 0);
                _warningTime    = new TimeSpan(     0,      WarningMinutes,    0);

                if (!IsStarted)
                    _remainingTime = _startTime;

                UpdateDisplay();
            }

            public void UpdateDisplay()
            {
                //if (_remainingTime <= _twoSeconds)
                //    Debugger.Break();    
                if (!_isPaused)
                    if (_remainingTime == _warningTime)
                        _player.Play(Sound, Volume);                            // Warning before end of Round
                    else
                        if (_isAtBreak && _remainingTime == _twoMinutes)
                            _player.Play(Sound, Volume);                        // Warning two minutes before end of Break
                        else
                            if (_remainingTime == TimeSpan.Zero)
                                if (!_isAtBreak && Round == BreakAfterRound)
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
                                        Round++;
                                        _timer.Stop();
                                    }
                                    else
                                        if (_isAtTransition || _isAtBreak)
                                        {
                                            _player.Play("Ding Ding", Volume);  // Start next round
                                            _remainingTime  = _startTime;
                                            _isAtTransition = false;
                                            _isAtBreak      = false;
                                            Round++;
                                        }
                                        else
                                            if (_transitionTime == TimeSpan.Zero)
                                            {
                                                _player.Play(Sound, Volume);    // End of round
                                                _remainingTime = _startTime;
                                                Round++;
                                            }
                                            else
                                            {
                                                _player.Play(Sound, Volume);    // Transition
                                                _isAtTransition = true;
                                                _remainingTime  = _transitionTime;
                                            }

                if (_remainingTime == TimeSpan.MinValue)
                    _remainingTime =  _startTime = new TimeSpan(Hours,                                                 Minutes, Seconds);

                Time = _remainingTime.ToString(                _remainingTime <  _oneHour ? @"mm\:ss" : @"hh\:mm\:ss");

                if (Round >  Rounds)
                    WarningVisiblity = Visibility.Collapsed;
                else
                    if (_isAtBreak)
                        WarningVisiblity = (_remainingTime >  TimeSpan.Zero && _remainingTime <  _twoMinutes && _breakTime >= _twoMinutes) ? Visibility.Visible : Visibility.Collapsed;
                    else
                        WarningVisiblity = (_remainingTime >  TimeSpan.Zero && _remainingTime <  _warningTime) ? Visibility.Visible : Visibility.Collapsed;

                Info = $"Vi spiller {Rounds} {getRoundsText} af {BoardsPerRound} spil";

                if (Round == Rounds && !TeamMatch)
                {
                    RoundText = $"Sidste runde!";
                    Info      = string.Empty;
                }
                else
                    if (Round <= Rounds)
                        if (_isAtBreak)
                            RoundText = $"Pause!";
                        else
                        {
                            if (Round <= BreakAfterRound)
                                Info += Environment.NewLine + $"Pause efter {BreakAfterRound}. {getRoundText}";

                            if (_isAtTransition)
                                RoundText = $"Der skiftes til {Round + 1}. {getRoundText}";
                            else
                                RoundText = $"{Round}. {getRoundText}";
                        }
                    else
                    {
                        RoundText = EndGreetingTop
                                 ?? $"Tak for god ro og orden.";
                        Info      = EndGreetingBottom
                                 ?? "Husk at rydde op på og "
                                  //+ Environment.NewLine
                                  + "omkring bordet";
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

            public void Close()
            {
                var result = MessageBox.Show("Hvis du lukker uret, så nulstilles det. Vil du lukke uret?", "Bekræftelse", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    StopCountdown();
                    Visibility = Visibility.Collapsed;
                    Configuration.Save();
                }
            }

            public void Start()
            {
                if (!_isStarted || _isPaused)
                {
                    _isStarted = true;
                    _isPaused  = false;
                    _timer.Start();
                }
            }

            public void Back()
            {
                if (Round == 1)
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
                        if (_isAtTransition)
                        {
                            _isAtTransition = false;
                            _remainingTime  = _startTime;
                        }
                        else
                            if (_startTime.Subtract(_remainingTime) >  _threshold)
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

                UpdateDisplay();
            }

            public void Pause()
            {
                if (_isStarted)
                    if (_isPaused)
                        Start();
                    else
                    {
                        _isPaused = true;
                        _timer.Stop();
                    }
            }

            public void Forward()
            {
                if (Round <= Rounds)
                {
                    if (_isAtBreak)
                    {
                        _isAtBreak = false;
                        Round++;
                        _remainingTime = _startTime;
                    }
                    else
                        if (_remainingTime >  TimeSpan.Zero)
                            if (!_isAtBreak && Round == BreakAfterRound)
                            {
                                _remainingTime = _breakTime;
                                _isAtBreak     = true;
                            }
                            else
                            {
                                Round++;
                                _remainingTime  = _startTime;
                                _isAtTransition = false;
                            }
                }

                UpdateDisplay();
            }

            public void LessTime()
            {
                _remainingTime = DBFMath.Max(TimeSpan.Zero, _remainingTime.Add(TimeSpan.FromSeconds(-30)));
                UpdateDisplay();
            }

            public void MoreTime()
            {
                _remainingTime = _remainingTime.Add(TimeSpan.FromSeconds(60));
                UpdateDisplay();
            }

            public void Reset(bool ask = true)
            {
                if (!ask
                ||  MessageBoxResult.OK == MessageBox.Show( "Hvis du nulstiller uret, indlæses indstillingerne på ny. Vil du fortsætte?"
                                                          , "Bekræftelse"
                                                          , MessageBoxButton.OKCancel
                                                          , MessageBoxImage.Question))
                {
                    StopCountdown();

                    _isAtBreak      = false;
                    _isAtTransition = false;
                    _isPaused       = true;
                    _isStarted      = false;
                    _remainingTime  = _startTime;
                    _startTime      = new TimeSpan(Hours, Minutes,           Seconds);
                    _transitionTime = new TimeSpan(0,     TransitionMinutes, 0);
                    _breakTime      = new TimeSpan(0,     BreakMinutes,      0);
                    Round           = 1;

                    UpdateDisplay();
                }
            }

            public BridgeTimerState CurrentState =>
                new BridgeTimerState()
                {
                    IsStarted = _isStarted
                  , IsAtBreak = _isAtBreak
                  , IsAtTransition = _isAtTransition
                  , RemainingTime = _remainingTime
                  , Round = this.Round
                };

            public void Restart(BridgeTimerState state)
            {
                StopCountdown();

                _isStarted      = state.IsStarted;
                _isPaused       = true;
                _isAtBreak      = state.IsAtBreak;
                _isAtTransition = state.IsAtTransition;
                _remainingTime  = state.RemainingTime;
                Round           = state.Round;
                //
                _startTime      = new TimeSpan(Hours, Minutes,           Seconds);
                _transitionTime = new TimeSpan(0,     TransitionMinutes, 0);
                _breakTime      = new TimeSpan(0,     BreakMinutes,      0);

                UpdateDisplay();
            }
        #endregion

        #region Private Timer Events         
            private void StopCountdown()
            {
                _timer.Stop();
                _isPaused = true;
                UpdateDisplay();
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                if (!_isPaused && _remainingTime.TotalSeconds >  0)
                {
                    _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                    UpdateDisplay();
                }
                else
                    if (_remainingTime.TotalSeconds <= 0)
                        _timer.Stop();
            }

            private string getRoundText  => TeamMatch ? "halvleg" : "runde";
            private string getRoundsText => TeamMatch ? "halvlege" : "runder";
        #endregion
    }
}
