using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Scores")]     public class Scores
    {
        [XmlElement(ElementName = "Half")] public Half Half { get; set; }
    }
}
