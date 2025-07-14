using System.Xml.Serialization;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Resultlist")]     public class Resultlist
    {
        public decimal ScoreTransferred => ScoreTransferredStr.AsDecimal();
        public decimal ScoreRegulated   => ScoreRegulatedStr.AsDecimal();

        //-----
        [XmlElement(ElementName = "ScoreTransferred")]  public string     ScoreTransferredStr { get; set; }
        [XmlElement(ElementName = "Pair")]              public List<Pair> Pairs               { get; set; }
        [XmlElement(ElementName = "ScoreRegulated")]    public string     ScoreRegulatedStr   { get; set; }
        [XmlElement(ElementName = "SectionResultNote")] public string     SectionResultNote   { get; set; }
        [XmlElement(ElementName = "Team")]              public List<Team> Teams               { get; set; }
    }
}
