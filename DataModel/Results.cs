using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Results")]    public class Results
    {
        [XmlElement(ElementName = "Team")]        public List<Team> Teams { get; set; }
    }
}
