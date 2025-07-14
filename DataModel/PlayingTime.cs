using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "PlayingTime")]     public class PlayingTime : IEquatable<PlayingTime>
    {
        [XmlAttribute(AttributeName = "Date")] public string                DateStr            { get; set; }
        [XmlElement("GroupTournament")]        public List<GroupTournament> TournamentFiles { get; set; }

        //-----
        public DateTime Date              => DateTime.Parse(DateStr);

        //-----
        public override string ToString() => string.IsNullOrEmpty(DateStr) ? null : Date.ToShortDateString() + " " + Date.ToShortTimeString();

        public override bool Equals(object obj)
        {
            if (obj is PlayingTime other)
                return DateStr == other.DateStr;

            return false;
        }

        public bool Equals(PlayingTime other)
        {
            return DateStr == other.DateStr;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(DateStr);
        }
    }
}