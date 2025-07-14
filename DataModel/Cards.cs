using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DBF.DataModel
{

    //[XmlRoot(ElementName = "North")]
    public class Cards
    {
        [XmlElement(ElementName = "Spades")]   public string Spades   { get; set; }
        [XmlElement(ElementName = "Hearts")]   public string Hearts   { get; set; }
        [XmlElement(ElementName = "Diamonds")] public string Diamonds { get; set; }
        [XmlElement(ElementName = "Clubs")]    public string Clubs    { get; set; }
    }

    [XmlRoot(ElementName = "Boards")]    
    public class Boards
    {
        [XmlElement(ElementName = "Boardspec")] public Boardspec Boardspec { get; set; }
    }
}
