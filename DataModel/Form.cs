using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Form")]     public class Form
    {
        [XmlAttribute(AttributeName = "No")] public string No   { get; set; }
        [XmlText]                            public string Text { get; set; }
    }
}