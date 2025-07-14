using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Section")]     public class SectionFile : IEquatable<SectionFile>
    {
        public int No => NoStr.AsInt(1);

        //-----
        [XmlElement(ElementName = "FileName")] public string FileName { get; set; }
        [XmlAttribute(AttributeName = "No")]   public string NoStr    { get; set; }
        [XmlElement(ElementName = "Date")]     public string Date     { get; set; }
        [XmlElement(ElementName = "Start")]    public string Start    { get; set; }
        [XmlElement(ElementName = "End")]      public string End      { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is SectionFile other)
                return  No   == other.No
                     &&  Date == other.Date;

            return false;
        }

        public bool Equals(SectionFile other)
        {
            return  No   == other.No
                 &&  Date == other.Date;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(No, Date);
        }
    }
}
