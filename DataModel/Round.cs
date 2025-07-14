using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Round")]     public class Round : IEquatable<Round>
    {
        public bool RoundCompleted => RoundCompletedStr.AsBool();
        public int  HalfNo         => HalfNoStr.AsInt();

        //-----
        [XmlElement(ElementName = "RoundCompleted")] public string       RoundCompletedStr { get; set; }
        [XmlElement(ElementName = "Scores")]         public Scores       Scores            { get; set; }
        [XmlAttribute(AttributeName = "No")]         public string       id                { get; set; }
        [XmlElement(ElementName = "HalfNo")]         public string       HalfNoStr         { get; set; }
        [XmlElement(ElementName = "AvgHAC")]         public string       AvgHAC            { get; set; }
        [XmlElement(ElementName = "Startlist")]      public Startlist    Startlist         { get; set; }
        [XmlElement(ElementName = "Resultlist")]     public Resultlist   Resultlist        { get; set; }
        [XmlElement(ElementName = "ButlerResult")]   public ButlerResult ButlerResult      { get; set; }
        [XmlElement(ElementName = "HACResult")]      public HACResult    HACResult         { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is Round other)
                return id == other.id;

            return false;
        }

        public bool Equals(Round other)
        {
            return id == other.id;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(id);
        }
    }
}
