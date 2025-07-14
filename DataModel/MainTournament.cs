using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "MainTournament")]     public class MainTournament : IEquatable<MainTournament>
    {
        [XmlAttribute(AttributeName = "Name")]    public string            Name        { get; set; }
        [XmlAttribute(AttributeName = "Id")]      public string            Id          { get; set; }
        [XmlElement(ElementName = "Description")] public string            Description { get; set; }
        [XmlElement(ElementName = "Form")]        public Form              Form        { get; set; }
        [XmlElement("PlayingTime")]               public List<PlayingTime> PlayingTime { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is MainTournament other)
                return Id == other.Id;

            return false;
        }

        public bool Equals(MainTournament other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(Id);
        }
     
        
    }
}