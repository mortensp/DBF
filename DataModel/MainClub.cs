using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "MainClub")]     public class MainClub
    {
        [XmlAttribute(AttributeName = "Name")] public string     Name  { get; set; }
        [XmlElement("Club")]                   public List<Club> Clubs { get; set; }
        [XmlIgnore]                            public int        No    { get; set; }
        [XmlIgnore]                            public string     Path  { get; set; }
    }
}