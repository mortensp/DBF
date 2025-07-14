using Caliburn.Micro;
//using RtfPipe.Tokens;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;

namespace DBF.DataModel
{
    public class Preset : PropertyChangedBase
    {
        private string name;
        private bool   customPreset;

        [JsonConstructorAttribute]
        public Preset( string name = null
                     , bool customPreset = true
                     , bool teamMatch = false
                     , int rounds = 9
                     , int boardsPerRound = 3
                     , int breakAfterRound = 5
                     , int hours = 0
                     , int minutes = 26
                     , int seconds = 0
                     , int transitionMinutes = 1
                     , int breakMinutes = 12
                     , int warningMinutes = 5
                     )
        {
            Name              = name?.ToString();
            CustomPreset      = customPreset;
            TeamMatch         = teamMatch;
            Rounds            = rounds;
            BoardsPerRound    = boardsPerRound;
            BreakAfterRound   = breakAfterRound;
            Hours             = hours;
            Minutes           = minutes;
            Seconds           = seconds;
            TransitionMinutes = transitionMinutes;
            BreakMinutes      = breakMinutes;
            WarningMinutes    = warningMinutes;
        }

        public Preset(Preset other)
        {
            Name              = other.Name?.ToString();
            CustomPreset      = true;
            TeamMatch         = other.TeamMatch;
            Rounds            = other.Rounds;
            BoardsPerRound    = other.BoardsPerRound;
            BreakAfterRound   = other.BreakAfterRound;
            Hours             = other.Hours;
            Minutes           = other.Minutes;
            Seconds           = other.Seconds;
            TransitionMinutes = other.TransitionMinutes;
            BreakMinutes      = other.BreakMinutes;
            WarningMinutes    = other.WarningMinutes;
        }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value?.ToString();
                    //NotifyOfPropertyChange(nameof(Name));
                }
            }
        }

        public bool CustomPreset
        {
            get => customPreset;
            set
            {
                if (customPreset != value)
                {
                    customPreset = value;
                    //NotifyOfPropertyChange(nameof(customPreset));
                }
            }
        }

        public bool TeamMatch         { get; set; }
        public int  Rounds            { get; set; }
        public int  BoardsPerRound    { get; set; }
        public int  BreakAfterRound   { get; set; }
        public int  Hours             { get; set; }
        public int  Minutes           { get; set; }
        public int  Seconds           { get; set; }
        public int  TransitionMinutes { get; set; }
        public int  BreakMinutes      { get; set; }
        public int  WarningMinutes    { get; set; }

        public bool Matches(Preset other)
        {
            return  (other.Name        is null || Name is null || Name == other.Name)
                 &&  TeamMatch         == other.TeamMatch
                 &&  Rounds            == other.Rounds
                 &&  BoardsPerRound    == other.BoardsPerRound
                 &&  BreakAfterRound   == other.BreakAfterRound
                 &&  Hours             == other.Hours
                 &&  Minutes           == other.Minutes
                 &&  Seconds           == other.Seconds
                 &&  TransitionMinutes == other.TransitionMinutes
                 &&  BreakMinutes      == other.BreakMinutes
                 &&  WarningMinutes    == other.WarningMinutes;
        }

        override public string ToString() => Name;

        internal void Update(Preset other)
        {
            CustomPreset      = true;
            TeamMatch         = other.TeamMatch;
            Rounds            = other.Rounds;
            BoardsPerRound    = other.BoardsPerRound;
            BreakAfterRound   = other.BreakAfterRound;
            Hours             = other.Hours;
            Minutes           = other.Minutes;
            Seconds           = other.Seconds;
            TransitionMinutes = other.TransitionMinutes;
            BreakMinutes      = other.BreakMinutes;
            WarningMinutes    = other.WarningMinutes;
        }
    }
}