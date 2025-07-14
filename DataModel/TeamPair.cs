using System.Xml.Serialization;

namespace DBF.DataModel
{
    public class TeamPair : Pair, IEquatable<TeamPair>
    {
        [XmlElement(ElementName = "NorthPlayer")] public TeamPlayer NorthPlayer { get; set; }
        [XmlElement(ElementName = "SouthPlayer")] public TeamPlayer SouthPlayer { get; set; }
        [XmlElement(ElementName = "EastPlayer")]  public TeamPlayer EastPlayer  { get; set; }
        [XmlElement(ElementName = "WestPlayer")]  public TeamPlayer WestPlayer  { get; set; }


        //-----
        public override string ToString()
        {
            if (Players is null || Players.Count() == 0)
                return "Oversidere"; // string.Empty;

            if (Players.Count() == 1)
                return $"{PairNo,2} {Players[0].Name} - ?";

            return $"{PairNo,2} {Players[0].Name} - {Players[1].Name}";
        }

        public new string PairName
        {
            get
            {
                if (NorthPlayer?.Name is not null || SouthPlayer?.Name is not null)
                    return $"{NorthPlayer.Name} - {SouthPlayer.Name}";

                if (EastPlayer?.Name is not null || WestPlayer?.Name is not null)
                    return $"{EastPlayer.Name} - {WestPlayer.Name}";

                return "Oversiddere";
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is TeamPair other)
                return PairNo == other.PairNo;

            return false;
        }

        public bool Equals(TeamPair other)
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
