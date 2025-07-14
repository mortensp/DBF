using DBF.DataModel;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Section")]     public class Section
    {
        public                                             int    SectionNo    { get; set; }

        //-----
        [XmlElement(ElementName = "Test")]          public string Test         { get; set; }
        [XmlAttribute(AttributeName = "Id")]        public string Id           { get; set; }
        [XmlAttribute(AttributeName = "SectionNo")] public string SectionNoStr { private get => SectionNo.ToString(); set => SectionNo = value.AsInt(); }
        [XmlAttribute(AttributeName = "Section")]   public string SectionNo2   { private get => SectionNo.ToString(); set => SectionNo = value.AsInt(); }         // Håndtering af afvigende navne i XML
    }
}