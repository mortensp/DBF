using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "TournamentType")]     public class TournamentType
    {
        [XmlAttribute(AttributeName = "Type")] public string Type { get; set; }
        [XmlText]                              public string Text { get; set; }
    }
}
