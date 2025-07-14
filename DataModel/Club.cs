using System;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Club")]     public class Club : IEquatable<Club>
    {
        [XmlAttribute(AttributeName = "Name")]     public string               Name           { get; set; }
        [XmlAttribute(AttributeName = "Id")]       public string               Id             { get; set; }
        [XmlAttribute(AttributeName = "GameDay")]  public string               GameDay        { get; set; }
        [XmlAttribute(AttributeName = "GameTime")] public string               GameTime       { get; set; }
        [XmlElement("MainTournament")]             public List<MainTournament> MainTournaments { get; set; }

        //-----
        public override string ToString() => Name;

        public override bool Equals(object obj)
        {
            if (obj is Club other)
                return Id == other.Id;

            return false;
        }

        public bool Equals(Club other)
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