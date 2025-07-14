//using Azure.Core.Pipeline;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Media;
using System.Xml.Serialization;
//using DeepCopy;

namespace DBF.DataModel
{

    //[DebuggerDisplay("{PairNo,2} {Players[0].TeamName} - {Players[1].TeamName}")]     [XmlRoot(ElementName = "Pair")]
    public class Pair : IEquatable<Pair>
    {

        //public Pair()
        //{
                
        //}

        ////[DeepCopyConstructor]
        //public Pair(Pair other)
        //{
        //    Title = other.Title;
        //    PairNoStr = other.PairNoStr;
        //    Players = new List<Player>(other.Players.Select(p => new Player(p)));
        //}

        public int     PairNo             => PairNoStr.AsInt();
        public int     PairTournamentRank => PairTournamentRankStr.AsInt();
        public int     SectionRank        => SectionRankStr.AsInt();
        public int     TournamentRank     => TournamentRankStr.AsInt();
        public int     HACRankSection     => HACRankSectionStr.AsInt();
        public decimal AvgHAC             => AvgHACStr.AsDecimal();
        public decimal ExpectedScore      => ExpectedScoreStr.AsDecimal();
        public decimal ActualScore        => ActualScoreStr.AsDecimal();
        public decimal Result             => ResultStr.AsDecimal();
        public int     Rank               => RankStr.AsInt();
        public decimal DeltaHAC           => DeltaHACStr.AsDecimal();
        public decimal TournamentScore    => TournamentScoreStr.AsDecimal();
        public decimal TournamentResult   => TournamentResultStr.AsDecimal();
        public decimal HACTotal           => HACTotalStr.AsDecimal();
        public int     HACRankTotal       => HACRankTotalStr.AsInt();
        public                                                    string       Title                 { get; set; }

        ///-----
        [XmlAttribute(AttributeName = "No")]               public string       PairNoStr             { get; set; }
        [XmlElement(ElementName = "PairTournamentRank")]   public string       PairTournamentRankStr { get; set; }
        [XmlElement(ElementName = "SectionRank")]          public string       SectionRankStr        { get; set; }
        [XmlElement(ElementName = "TournamentRank")]       public string       TournamentRankStr     { get; set; }
        [XmlElement(ElementName = "HACRankSection")]       public string       HACRankSectionStr     { get; set; }
        [XmlElement(ElementName = "AvgHAC")]               public string       AvgHACStr             { get; set; }
        [XmlElement(ElementName = "ExpectedScore")]        public string       ExpectedScoreStr      { get; set; }
        [XmlElement(ElementName = "StartPos")]             public string       StartPos              { get; set; }
        [XmlElement(ElementName = "ActualScore")]          public string       ActualScoreStr        { get; set; }
        [XmlElement(ElementName = "Result")]               public string       ResultStr             { get; set; }
        [XmlAttribute(AttributeName = "Rank")]             public string       RankStr               { get; set; }
        [XmlElement(ElementName = "MP")]                   public string       MP                    { get; set; }
        [XmlElement(ElementName = "DeltaHAC")]             public string       DeltaHACStr           { get; set; }
        [XmlElement(ElementName = "Player")]               public List<Player> Players               { get; set; }
        [XmlElement(ElementName = "TournamentScore")]      public string       TournamentScoreStr    { get; set; }
        [XmlElement(ElementName = "TournamentResult")]     public string       TournamentResultStr   { get; set; }
        [XmlElement(ElementName = "HACTotal")]             public string       HACTotalStr           { get; set; }
        [XmlElement(ElementName = "HACRankTotal")]         public string       HACRankTotalStr       { get; set; }
        [XmlElement(ElementName = "Direction")]            public string       Direction             { get; set; }
        [XmlElement(ElementName = "Comment")]              public string       Comment               { get; set; }
        [XmlElement(ElementName = "TeamNumber")]           public string       TeamNumber            { get; set; }
        [XmlElement(ElementName = "TeamName")]             public string       TeamName              { get; set; }
        [XmlElement(ElementName = "PairPoints")]           public string       PairPoints            { get; set; }
        [XmlElement(ElementName = "PairPct")]              public string       PairPct               { get; set; }
        [XmlElement(ElementName = "PairRank")]             public string       PairRank              { get; set; }
        [XmlElement(ElementName = "PairTournamentPoints")] public string       PairTournamentPoints  { get; set; }
        [XmlElement(ElementName = "PairTournamentPct")]    public string       PairTournamentPct     { get; set; }
        [XmlAttribute(AttributeName = "Bye")]              public string       Bye                   { get; set; }
        [XmlElement(ElementName = "PairResultIMPs")]       public string       PairResultIMPs        { get; set; }
        [XmlElement(ElementName = "PairResultButler")]     public string       PairResultButler      { get; set; }
        [XmlElement(ElementName = "HACDelta")]             public string       HACDelta              { get; set; }

        // Håndtering af afvigende navne i XML
        [XmlAttribute(AttributeName = "Direction")]         public string DirectionStr
        {
            set
            {
                if (string.IsNullOrEmpty(Direction))
                    Direction = value;
            }
        }

        // ---
        public string PairName
        {
            get
            {
                if (Players.Count == 0)
                    return "Oversiddere";

                if (Players.Count == 1)
                    return $"{Players[0].Name} - ?";

                if (Players.Count == 2)
                    return $"{Players[0].Name} - {Players[1].Name}";

                var names = Players[0].ToString();

                for (int i = 1; i <  Players.Count; i++)
                    names += $", {Players[i].Name}";

                return names;
            }
        }

        public override string ToString()
        {
            if (Players is null || Players.Count() == 0)
                return string.Empty;

            if (Players.Count() == 1)
                return $"{PairNo,2} {Players[0].Name} - ?";

            return $"{PairNo,2} {Players[0].Name} - {Players[1].Name}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Pair other)
                return PairNo == other.PairNo;

            return false;
        }

        public bool Equals(Pair other)
        {
            return PairNo == other.PairNo;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(PairNo);
        }
    }
}
