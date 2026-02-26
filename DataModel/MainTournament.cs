using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "MainTournament")]     public class MainTournament : IEquatable<MainTournament>
    {
        // Regex mønster for datoformatet yyyy-MM-dd
        private static string pattern = @"\b\d{4}-\d{2}-\d{2}\b(\d{2}:\d{2})?";
        private string name;

        [XmlAttribute(AttributeName = "Name")] public string Name
        {
            get => name;
            set
            {
                name = value;
                ShortName = Regex.Replace(value, pattern, "").Trim();
            }
        }
        [XmlAttribute(AttributeName = "Id")]      public string            Id          { get; set; }
        [XmlElement(ElementName = "Description")] public string            Description { get; set; }
        [XmlElement(ElementName = "Form")]        public Form              Form        { get; set; }
        [XmlElement("PlayingTime")]               public List<PlayingTime> PlayingTime { get; set; }

        //-----
        [XmlIgnore]
        public string ShortName        {            get; set;        }

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
