using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Team")]     public class Team : IEquatable<Team>
    {
        public double  AvgHAC         => AvgHACStr.AsDouble();
        public int     TeamNo         => TeamNoStr.AsInt();
        public int     ImpScore       => ImpScoreStr.AsInt();
        public double? KP             => KPStr is null ? null : KPStr.AsDouble();
        public int     OpponentTeamNo => OpponentTeamNoStr.AsInt();
        public                                                  string       Title              { get; set; }
        public                                                  double       TotalKP            { get; set; }
        public                                                  int          TournamentRank     { get; set; }

        //-----
        [XmlElement(ElementName = "Name")]               public string       TeamName           { get; set; }
        [XmlElement(ElementName = "StartPos")]           public string       StartPos           { get; set; }
        [XmlElement(ElementName = "AvgHAC")]             public string       AvgHACStr          { get; set; }
        [XmlElement(ElementName = "Player")]             public List<Player> Players            { get; set; }
        [XmlAttribute(AttributeName = "No")]             public string       TeamNoStr          { get; set; }
        [XmlElement(ElementName = "ImpScore")]           public string       ImpScoreStr        { get; set; }
        [XmlElement(ElementName = "ImpAdjustment")]      public string       ImpAdjustment      { get; set; }
        [XmlElement(ElementName = "MatchPointScore")]    public string       MatchPointScore    { get; set; }
        [XmlElement(ElementName = "PenaltyScore")]       public string       PenaltyScore       { get; set; }
        [XmlElement(ElementName = "EndScore")]           public string       KPStr              { get; set; }
        [XmlElement(ElementName = "OpponentTeamNo")]     public string       OpponentTeamNoStr  { get; set; }
        [XmlElement(ElementName = "Half")]               public List<Half>   Halfs              { get; set; }
        [XmlElement(ElementName = "Tiebreak")]           public string       Tiebreak           { get; set; }
        [XmlElement(ElementName = "SectionTiebreak")]    public string       SectionTiebreak    { get; set; }
        [XmlElement(ElementName = "TournamentTiebreak")] public string       TournamentTiebreak { get; set; }

        // Håndtering af afvigende navne i XML
        [XmlElement(ElementName = "TeamName")]         public string TeamName2
        {
            set
            {
                if (TeamName == null)
                    TeamName =  value; // For at undgå at slette det eksisterende navn, hvis det er sat
            }
        }

        //-----
        // Dan et felt med alle navnene
        public string Names
        {
            get
            {
                var names = Players?.Select(p => p.NameWithSubstitute).ToList();
                return names != null ? string.Join(",  ", names).Trim() : string.Empty;
            }
        }

        public string NamesWithHac
        {
            get
            {
                var names = Players?.Select(p => p.NameWithHac).ToList();
                return names != null ? string.Join(",  ", names) : string.Empty;
            }
        }

        public string MatchResult
        {
            get
            {
                if (KP is null)
                    return string.Empty;

                var oppKP = 20 - KP;

                return $"{KP.Value,5:0.00}-{oppKP.Value,5:0.00}";
            }
        }

        #region Overrides
            public override string ToString() => TeamName;

            public override bool Equals(object obj)
            {
                if (obj is Team other)
                    return TeamNo == other.TeamNo;

                return false;
            }

            public bool Equals(Team other)
            {
                return TeamNo == other.TeamNo;
            }

            public override int GetHashCode()
            {
                // Brug de samme properties som i Equals
                return HashCode.Combine(TeamNo);
            }
        #endregion
    }
}
