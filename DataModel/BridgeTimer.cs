using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.ViewModels;

namespace DBF.DataModel
{
    public partial class BridgeTimer : TimerSetting
    {
        private DispatcherTimer _timer;
        //
        private static readonly TimeSpan _oneHour = new TimeSpan(1, 0, 0);
        private static readonly TimeSpan _fiveMinutes = new TimeSpan(0, 5, 0);
        private static readonly TimeSpan _twoMinutes = new TimeSpan(0, 2, 0);
        private TimeSpan _startTime = new TimeSpan(0, 21, 0);
        private TimeSpan _transitionTime = new TimeSpan(0, 1, 0);
        private TimeSpan _breakTime = new TimeSpan(0, 12, 0);
        private TimeSpan _warningTime = new TimeSpan(0, 5, 0);
        private TimeSpan _remainingTime = TimeSpan.MinValue;
        private bool _isStarted;
        private bool _isAtBreak = false;
        private bool _isAtTransition = false;
        private bool _isPaused;
        private int _round = 1;
        private Configuration configuration = null;

        public BridgeTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
        }

        #region Public Properties
        [JsonIgnore] public Configuration Configuration { get => configuration ?? (configuration = IoC.Get<Configuration>()); private set => configuration = value; }
        [JsonIgnore] public string Time { get; set; } = "21:17";
        [JsonIgnore] public Visibility WarningVisiblity { get; set; } = Visibility.Hidden;
        [JsonIgnore] public string Round { get; set; }
        [JsonIgnore] public string MoreInfo { get; set; }
        [JsonIgnore] public bool IsPaused => _isPaused;
        [JsonIgnore] public bool IsStarted => _isStarted;
        #endregion

        #region Public Methods
        public async void OpenSetting()
        {
            var screen = IoC.Get<TimerSettingViewModel>();
            var windowManager = IoC.Get<IWindowManager>();
            screen.Setting = this;
            await windowManager.ShowDialogAsync(screen);

            // If the dialog was enden with save, then our Settings was updated
            _startTime = new TimeSpan(Hours, Minutes, Seconds);
            _breakTime = new TimeSpan(0, BreakMinutes, 0);
            _transitionTime = new TimeSpan(0, TransitionMinutes, 0);
            _warningTime = new TimeSpan(0, WarningMinutes, 0);

            if (!IsStarted)
                _remainingTime = _startTime;

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (!_isPaused)
                if (_remainingTime == _warningTime)
                    AudioPlayer.Play(Sound, Volume);                        // Warning before end of Round
                else
                    if (_isAtBreak && _remainingTime == _twoMinutes)
                    AudioPlayer.Play(Sound, Volume);                        // Warning two minutes before end of Break
                else
                        if (_remainingTime == TimeSpan.Zero)
                    if (!_isAtBreak && _round == BreakAfterRound)
                    {
                        AudioPlayer.Play("Ding Ding", Volume);              // Break
                        _remainingTime = _breakTime;
                        _isAtTransition = false;
                        _isAtBreak = true;
                    }
                    else
                        if (_round >= Rounds)
                    {
                        AudioPlayer.Play(Sound, Volume);                    // End of game                                        
                        _round++;
                        _timer.Stop();
                    }
                    else
                            if (_isAtTransition || _isAtBreak)
                    {
                        AudioPlayer.Play("Ding Ding", Volume);              // Start next round
                        _remainingTime = _startTime;
                        _isAtTransition = false;
                        _isAtBreak = false;
                        _round++;
                    }
                    else
                                if (_transitionTime == TimeSpan.Zero)
                    {
                        AudioPlayer.Play(Sound, Volume);                    // End of round
                        _remainingTime = _startTime;
                        _round++;
                    }
                    else
                    {
                        AudioPlayer.Play(Sound, Volume);                    // Transition
                        _isAtTransition = true;
                        _remainingTime = _transitionTime;
                    }

            if (_remainingTime == TimeSpan.MinValue)
                _remainingTime = _startTime = new TimeSpan(Hours, Minutes, Seconds);

            Time = _remainingTime.ToString(_remainingTime < _oneHour ? @"mm\:ss" : @"hh\:mm\:ss");

            if (_isAtBreak)
                WarningVisiblity = (_remainingTime > TimeSpan.Zero && _remainingTime < _twoMinutes && _breakTime >= _twoMinutes) ? Visibility.Visible : Visibility.Collapsed;
            else
                WarningVisiblity = (_remainingTime > TimeSpan.Zero && _remainingTime < _warningTime) ? Visibility.Visible : Visibility.Collapsed;

            MoreInfo = string.Empty;

            if (_round == Rounds)
            {
                Round = $"Sidste runde!";
                Info = string.Empty;
            }
            else
                if (_round < Rounds)
                if (_isAtBreak)
                    Round = $"Pause!";
                else
                {
                    if (_round <= BreakAfterRound)
                        MoreInfo = $"Pause efter {BreakAfterRound}. {RoundText}";

                    if (_isAtTransition)
                        Round = $"Der skiftes til {_round + 1}. {RoundText}";
                    else
                        Round = $"{_round}. {RoundText}";
                }
            else
            {
                Round = $"Tak for i god ro og orden.";
                Info = "Husk at aflevere kort, melde-";
                MoreInfo = "kasser mm. ovre på reolen";
                Time = string.Empty;
            }
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
                _isPaused = false;
                //StartCountdown();
                _timer.Start();
            }
        }

        public void Back()
        {
            StopCountdown();

            if (_round == 1)
            {
                _isStarted = false;
                _remainingTime = _startTime;
            }
            else
                if (_isAtBreak)
                if (_remainingTime == _breakTime)
                {
                    _isAtBreak = false;
                    _remainingTime = _startTime;
                }
                else
                    _remainingTime = _breakTime;
            else
                    if (_isAtTransition || _remainingTime < _startTime)
            {
                _isAtTransition = false;
                _remainingTime = _startTime;
            }
            else
                        if (_remainingTime < _startTime)
                _remainingTime = _startTime;
            else
            {
                _round--;

                if (_round == BreakAfterRound)
                {
                    _remainingTime = _breakTime;
                    _isAtBreak = true;
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
                {
                    _isPaused = false;
                    Start();
                }
                else
                {
                    _isPaused = true;
                    _timer.Stop();
                }
        }

        public void Forward()
        {
            StopCountdown();

            if (_round <= Rounds)
            {
                if (_isAtBreak)
                {
                    _isAtBreak = false;
                    _round++;
                    _remainingTime = _startTime;
                }
                else
                    if (_remainingTime > TimeSpan.Zero)
                    if (!_isAtBreak && _round == BreakAfterRound)
                    {
                        _remainingTime = _breakTime;
                        _isAtBreak = true;
                    }
                    else
                    {
                        _round++;
                        _remainingTime = _startTime;
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

        public void Reset()
        {
            if (_isStarted)
            { }

            var result = MessageBox.Show("Hvis du nulstiller uret, indlæses indstillingerne på ny. Vil du fortsætte?", "Bekræftelse", MessageBoxButton.OKCancel, MessageBoxImage.Question);

            if (result == MessageBoxResult.OK)
            {
                StopCountdown();
                _isAtBreak = false;
                _isAtTransition = false;
                _isPaused = true;
                _isStarted = false;
                _isStarted = false;
                _remainingTime = _startTime;
                _startTime = new TimeSpan(Hours, Minutes, Seconds);
                _transitionTime = new TimeSpan(0, TransitionMinutes, 0);
                _breakTime = new TimeSpan(0, BreakMinutes, 0);
                _round = 1;

                Info = $"Vi spiller {Rounds} {RoundsText} af {BoardsPerRound} spil";
                UpdateDisplay();
            }
        }
        #endregion

        #region Private Timer Events
        //private void StartCountdown()
        //{
        //    _isStarted = true;
        //    _isPaused  = false;

        //    //_timer.Start();
        //}

        private void StopCountdown()
        {
            _timer.Stop();
            _isPaused = true;
            UpdateDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_isPaused && _remainingTime.TotalSeconds > 0)
            {
                _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));

                UpdateDisplay();
            }
            else
                if (_remainingTime.TotalSeconds <= 0)
                _timer.Stop();
        }

        private string RoundText => TeamMatch ? "halvleg" : "runde";

        private string RoundsText => TeamMatch ? "halvlege" : "runder";

        //private void updateAll()
        //{
        //    Time = _remainingTime <  _oneHour
        //         ? _remainingTime.ToString(@"mm\:ss")
        //         : _remainingTime.ToString(@"mm\:ss");
        //}
        #endregion
    }
}
