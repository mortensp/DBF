using System.Xml.Serialization;

namespace DBF.DataModel
{
    public class TeamPlayer
    {
        [XmlElement(ElementName = "Name")]     public string Name     { get; set; }
        [XmlElement(ElementName = "MemberID")] public string MemberID { get; set; }
        [XmlElement(ElementName = "HACStart")] public string HACStart { get; set; }
    }
}