using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "ButlerResult")]     public class ButlerResult
    {
        [XmlElement(ElementName = "Team")]         public List<Team> Teams { get; set; }
    }
}
