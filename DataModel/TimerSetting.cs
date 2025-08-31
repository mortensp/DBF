using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using static System.TimeZoneInfo;
using Brush = System.Windows.Media.Brush;

namespace DBF.DataModel
{
    public class TimerSetting : Preset
    {
        private Color? color;
        //private Brush  background = Brushes.White;
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
                               , Color? color = null
                               , string sound = null
                               , int volume = 50
                               , Visibility visibility = Visibility.Visible
                               ) : base(name, customPreset, teamMatch, rounds, boardsPerRound, breakAfterRound, hours, minutes, seconds, transitionMinutes, breakMinutes, warningMinutes)
            {
                Groups     = groups;
                Info       = info;
                Sound      = sound;
                Volume     = volume;
                Color      = color;
                Visibility = visibility;
            }
        #endregion

        public Color? Color
        {
            get=> color;
            set
            {
                if (Set(ref color, value))
                    if (value is null)
                        Background = new SolidColorBrush(Colors.White);
                    else
                        Background = new SolidColorBrush((Color)value);
            }
        }

        public              GroupFlags Groups     { get; set; }
        public              string     Info       { get; set; }
        [JsonIgnore] public Brush      Background { get; set; }
        public              string     Sound      { get; set; }
        public              int        Volume     { get; set; }
        public              Visibility Visibility { get; set; }

        public              string     GroupStr   { get => Groups.ToFriendlyString(); }

        public new void Update(Preset preset)
        {
            Name              = preset.Name;
            CustomPreset      = preset.CustomPreset;
            TeamMatch         = preset.TeamMatch;
            Rounds            = preset.Rounds;
            BoardsPerRound    = preset.BoardsPerRound;
            BreakAfterRound   = preset.BreakAfterRound;
            Hours             = preset.Hours;
            Minutes           = preset.Minutes;
            Seconds           = preset.Seconds;
            TransitionMinutes = preset.TransitionMinutes;
            BreakMinutes      = preset.BreakMinutes;
            WarningMinutes    = preset.WarningMinutes;

            if (preset is TimerSetting tSetting)
            {
                Groups     = tSetting.Groups;
                Info       = tSetting.Info;
                Volume = 0;
                Sound      = tSetting.Sound;
                Volume     = tSetting.Volume;
                Color      = tSetting.Color;
                Visibility = tSetting.Visibility;
            }
        }
    }
}