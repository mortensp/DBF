using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "HACResult")]     public class HACResult
    {
        [XmlElement(ElementName = "Team")]        public List<Team> Teams { get; set; }
    }
}