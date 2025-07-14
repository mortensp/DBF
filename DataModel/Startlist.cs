using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Startlist")]     public class Startlist
    {
        [XmlElement(ElementName = "Pair")]             public List<Pair> Pairs            { get; set; }
        [XmlElement(ElementName = "SectionStartNote")] public string     SectionStartNote { get; set; }
        [XmlElement(ElementName = "Team")]             public List<Team> Teams            { get; set; }
    }
}
