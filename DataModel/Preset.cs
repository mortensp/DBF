using Caliburn.Micro;

using DBF.Converters;
using DBF.Helpers;

using System;
using System.Text.Json.Serialization;

namespace DBF.DataModel
{
    public class Preset : PropertyChangedBase
    {
        private string key;
        private bool   customPreset;

        // Parameterless constructor for JSON deserialization
        public Preset() { }

        public Preset(string key = null
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
            Key = key;
            CustomPreset = customPreset;
            TeamMatch = teamMatch;
            Rounds = rounds;
            BoardsPerRound = boardsPerRound;
            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
            TransitionMinutes = transitionMinutes;
            WarningMinutes = warningMinutes;

            setBreak(breakAfterRound, breakMinutes);
        }

        public Preset(Preset other)
        {
            Key = other.Key;
            CustomPreset = true;
            TeamMatch = other.TeamMatch;
            Rounds = other.Rounds;
            BoardsPerRound = other.BoardsPerRound;
            Hours = other.Hours;
            Minutes = other.Minutes;
            Seconds = other.Seconds;
            TransitionMinutes = other.TransitionMinutes;
            WarningMinutes = other.WarningMinutes;

            setBreak(other.BreakAfterRound, other.BreakMinutes);
        }

        [JsonPropertyName("Name")]
        [JsonConverter(typeof(PresetNameJsonConverter))]
        public string Key
        {
            get => key;
            set
            {
                if (key != value)
                    key = value;
            }
        }

        // Display name — translated dynamically based on CurrentCulture
        [JsonIgnore]
        public string Name => Key.GetTranslation();

        public bool CustomPreset
        {
            get => customPreset;
            set
            {
                if (customPreset != value)
                    customPreset = value;
            }
        }

        public bool IsHidden { get; set; }
        public bool TeamMatch { get; set; }
        public int Rounds { get; set; }
        public int BoardsPerRound { get; set; }
        public int BreakAfterRound { get; set; }

        public int Hours { get; set; }
        public int Minutes
        {
            get => field;
            set
            {
                if (value > 60)
                {
                    Hours = value / 60;
                    field = value % 60;
                }
                else
                    field = value;
            }
        }

        public int Seconds
        {
            get => field;
            set
            {
                if (value > 60)
                {
                    Minutes = value / 60;
                    field = value % 60;
                }
                else
                    field = value;
            }
        }

        public int TransitionMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public int WarningMinutes { get; set; }

        public double Duration => BreakMinutes
                                         + Rounds * (Hours * 60 + Minutes + Seconds / 60d)
                                         + (BreakAfterRound > 0 && BreakAfterRound < Rounds ? Rounds - 2 : Rounds - 1)
                                         * Math.Max(0, TransitionMinutes);

        // Matches uses an invariant key (not localized name)
        public bool Matches(Preset other, bool withoutName = false)
        {
            return (withoutName
                  || other.Key is null
                  || Key is null
                  || string.Equals(Key, other.Key, StringComparison.Ordinal))
                 && TeamMatch == other.TeamMatch
                 && Rounds == other.Rounds
                 && BoardsPerRound == other.BoardsPerRound
                 && BreakAfterRound == other.BreakAfterRound
                 && Hours == other.Hours
                 && Minutes == other.Minutes
                 && Seconds == other.Seconds
                 && TransitionMinutes == other.TransitionMinutes
                 && BreakMinutes == other.BreakMinutes
                 && WarningMinutes == other.WarningMinutes;
        }

        override public string ToString() => Name;

        internal void Update(Preset other)
        {
            CustomPreset = true;
            TeamMatch = other.TeamMatch;
            Rounds = other.Rounds;
            BoardsPerRound = other.BoardsPerRound;
            Hours = other.Hours;
            Minutes = other.Minutes;
            Seconds = other.Seconds;
            TransitionMinutes = other.TransitionMinutes;
            WarningMinutes = other.WarningMinutes;

            setBreak(other.BreakAfterRound, other.BreakMinutes);
        }

        internal void setBreak(int breakAfterRound, int breakMinutes)
        {
            if (breakAfterRound > 0
            && breakMinutes > 0)
            {
                BreakAfterRound = breakAfterRound;
                BreakMinutes = breakMinutes;
            }
            else
            {
                BreakAfterRound = 0;
                BreakMinutes = 0;
            }
        }
    }
}
