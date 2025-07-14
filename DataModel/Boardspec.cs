using System.Xml.Serialization;

namespace DBF.DataModel
{
   
    [XmlRoot(ElementName = "Boardspec")]    public class Boardspec
    {
        [XmlElement(ElementName = "Board")]  public List<Board> Boards { get; set; }
        [XmlAttribute(AttributeName = "No")] public string      No    { get; set; }
    }
}
