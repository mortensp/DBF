using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Table")]    
    public class Table : IEquatable<Table>
    {

        //-----
        [XmlElement(ElementName = "Pair")]   public List<Pair>  Pairs  { get; set; }
        [XmlElement(ElementName = "Board")]  public List<Board> Boards { get; set; }
        [XmlAttribute(AttributeName = "No")] public string      No    { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is Table other)
                return No == other.No;

            return false;
        }

        public bool Equals(Table other)
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
