using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace DBF.DataModel
{
    public class TimerSetting : Preset
    {
        private Color? fgColor;
        private Color? bgColor;

        #region Constructors
            public TimerSetting( string name = null
                               , bool customPreset = true
                               , bool teamMatch = false
                               , int rounds = 0
                               , int boardsPerRound = 0
                               , int breakAfterRound = 0
                               , int hours = 0
                               , int minutes = 0
                               , int seconds = 0
                               , int transitionMinutes = 0
                               , int breakMinutes = 0
                               , int warningMinutes = 0
                               //
                               , GroupFlags groups = 0
                               , string info = ""
                               , Color? foregroundColor = null
                               , Color? backgroundColor = null
                               , string sound = null
                               , int volume = 50
                               , Visibility visibility = Visibility.Visible
                               , string pauseMessage = null
                               , string endGreetingTop = null
                               , string endGreetingBottom = null
                               //, TimeOnly? startTime = null
                               ) : base(name, customPreset, teamMatch, rounds, boardsPerRound, breakAfterRound, hours, minutes, seconds, transitionMinutes, breakMinutes, warningMinutes)
            {
                Groups            = groups;
                Info              = info;
                Sound             = sound;
                Volume            = volume;
                BackgroundColor   = backgroundColor;
                ForegroundColor   = foregroundColor;
                Visibility        = visibility;
                PauseMessage      = pauseMessage;
                EndGreetingTop    = endGreetingTop;
                EndGreetingBottom = endGreetingBottom;
                //StartTime         = startTime;
            }
        #endregion

        public Color? ForegroundColor
        {
            get => fgColor;
            set
            {
                if (Set(                             ref fgColor,                                 value))
                    Foreground = new SolidColorBrush(value is null ? Colors.Black : (Color)value);
            }
        }

        public Color? BackgroundColor
        {
            get => bgColor;
            set
            {
                if (Set(ref bgColor, value))
                {
                    Background      = new SolidColorBrush(value is null ? Colors.White : (Color)value);
                    ForegroundColor = getContrastingColor(value ?? Colors.White);
                }
            }
        }

        public              GroupFlags Groups     { get; set; }
        public              string     Info       { get; set; }
        [JsonIgnore] public Brush      Foreground { get; set; }
        [JsonIgnore] public Brush      Background { get; set; }
        public              string     Sound      { get; set; }
        public              int        Volume     { get; set; }
        public              Visibility Visibility { get; set; }
        public              string     GroupStr   { get => Groups.ToFriendlyString(); }
        //[JsonIgnore] public TimeOnly?  StartTime  { get; set; }

        public string PauseMessage
        {
            get => field;
            set
            {
                value = string.IsNullOrWhiteSpace(value)
                      ? null
                      : field = value?.Trim();

                Set(ref field, value);
            }
        }

        public string EndGreetingTop
        {
            get => field;
            set
            {
                value = string.IsNullOrWhiteSpace(value)
                      ? null
                      : field = value?.Trim();

                Set(ref field, value);
            }
        }

        public string EndGreetingBottom
        {
            get => field;
            set
            {
                value = string.IsNullOrWhiteSpace(value)
                      ? null
                      : field = value?.Trim();

                Set(ref field, value);
            }
        }

        public new void Update(Preset preset)
        {
            Name            = preset.Name;
            CustomPreset    = preset.CustomPreset;
            TeamMatch       = preset.TeamMatch;
            Rounds          = preset.Rounds;
            BoardsPerRound  = preset.BoardsPerRound;
            BreakAfterRound = preset.BreakAfterRound;

            Hours             = preset.Hours;
            Minutes           = preset.Minutes;
            Seconds           = preset.Seconds;
            TransitionMinutes = preset.TransitionMinutes;
            BreakMinutes      = preset.BreakMinutes;
            WarningMinutes    = preset.WarningMinutes;

            if (preset is TimerSetting tSetting)
            {
                Groups            = tSetting.Groups;
                Info              = tSetting.Info;
                Volume            = 0;
                Sound             = tSetting.Sound;
                Volume            = tSetting.Volume;
                BackgroundColor   = tSetting.BackgroundColor;
                ForegroundColor   = tSetting.ForegroundColor;
                Visibility        = tSetting.Visibility;
                PauseMessage      = tSetting.PauseMessage;
                EndGreetingTop    = tSetting.EndGreetingTop;
                EndGreetingBottom = tSetting.EndGreetingBottom;
                //StartTime         = tSetting.StartTime;
            }
        }

        private static Color getContrastingColor(Color bgColor)
        {
            // Beregn luminans (per W3C standard)
            double luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255;

            // Hvis baggrunden er lys, brug sort tekst – ellers hvid
            return luminance >  0.5 ? Colors.Black : Colors.White;
        }
    }
}
