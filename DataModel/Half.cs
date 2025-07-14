using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Half")]     public class Half : IEquatable<Half>
    {
        public int No => NoStr.AsInt();

        //-----
        [XmlAttribute(AttributeName = "No")] public string         NoStr  { get; set; }
        [XmlElement(ElementName = "Table")]  public List<Table>    Tables { get; set; }
        [XmlElement(ElementName = "Pair")]   public List<TeamPair> Pairs  { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is Half other)
                return No == other.No;

            return false;
        }

        public bool Equals(Half other)
        {
            return No == other.No;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(No);
        }
    }
}

