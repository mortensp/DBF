//using BridgeTypes;
using System.Xml.Serialization;
using Syncfusion.Windows.Shared;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Board")]     public class Board : IEquatable<Board>
    {
        public decimal DatumScore => DatumScoreStr.AsDecimal();
        public int     TricksS    => TricksStr.AsInt();
        public decimal Score      => ScoreStr.AsDecimal();
        public decimal PointsNS   => PointsNSStr.AsDecimal();
        public decimal PointsEW   => PointsEWStr.AsDecimal();
        public decimal PctNS      => PctNSStr.AsDecimal();
        public decimal PctEW      => PctEWStr.AsDecimal();
        public int     No         => NoStr.AsInt();
        public int     NorthHCP   => NorthHCPStr.AsInt();
        public int     SouthHCP   => SouthHCPStr.AsInt();
        public int     EastHCP    => EastHCPStr.AsInt();
        public int     WestHCP    => WestHCPStr.AsInt();

        [XmlElement(ElementName = "Excluded")]         public   string Excluded           { get; set; }
        [XmlElement(ElementName = "DatumScore")]       public   string DatumScoreStr      { get; set; }
        [XmlElement(ElementName = "Declarer")]         public   string Declarer           { get; set; }
        [XmlElement(ElementName = "Contract")]         public   string Contract           { get; set; }
        [XmlElement(ElementName = "Doubling")]         public   string Doubling           { get; set; }
        [XmlElement(ElementName = "Tricks")]           public   string TricksStr          { get; set; }
        [XmlElement(ElementName = "Lead")]             public   string Lead               { get; set; }
        [XmlElement(ElementName = "Score")]            public   string ScoreStr           { get; set; }
        [XmlElement(ElementName = "PointsNS")]         public   string PointsNSStr        { get; set; }
        [XmlElement(ElementName = "PointsEW")]         public   string PointsEWStr        { get; set; }
        [XmlElement(ElementName = "PctNS")]            public   string PctNSStr           { get; set; }
        [XmlElement(ElementName = "PctEW")]            public   string PctEWStr           { get; set; }
        [XmlElement(ElementName = "AdjustmentStrNS")]  public   string AdjustmentStrNS    { get; set; }
        [XmlElement(ElementName = "AdjustmentDescNS")] public   string AdjustmentDescNS   { get; set; }
        [XmlElement(ElementName = "AdjustmentTypeNS")] public   string AdjustmentTypeNS   { get; set; }
        [XmlElement(ElementName = "AdjustmentStrEW")]  public   string AdjustmentStrEW    { get; set; }
        [XmlElement(ElementName = "AdjustmentDescEW")] public   string AdjustmentDescEW   { get; set; }
        [XmlElement(ElementName = "AdjustmentTypeEW")] public   string AdjustmentTypeEW   { get; set; }
        [XmlAttribute(AttributeName = "No")]           public   string NoStr              { get; set; }
        [XmlElement(ElementName = "Dealer")]           public   string Dealer             { get; set; }
        [XmlElement(ElementName = "Vulnerability")]    public   string Vulnerability      { get; set; }
        [XmlElement(ElementName = "DDInfoValid")]      public   string DDInfoValid        { get; set; }
        [XmlElement(ElementName = "ParContract")]      public   string ParContract        { get; set; }
        [XmlElement(ElementName = "NorthHCP")]         public   string NorthHCPStr        { get; set; }
        [XmlElement(ElementName = "SouthHCP")]         public   string SouthHCPStr        { get; set; }
        [XmlElement(ElementName = "EastHCP")]          public   string EastHCPStr         { get; set; }
        [XmlElement(ElementName = "WestHCP")]          public   string WestHCPStr         { get; set; }
        [XmlElement(ElementName = "NNT")]              public   string NNT                { get; set; }
        [XmlElement(ElementName = "NSP")]              public   string NSP                { get; set; }
        [XmlElement(ElementName = "NHE")]              public   string NHE                { get; set; }
        [XmlElement(ElementName = "NDI")]              public   string NDI                { get; set; }
        [XmlElement(ElementName = "NCL")]              public   string NCL                { get; set; }
        [XmlElement(ElementName = "SNT")]              public   string SNT                { get; set; }
        [XmlElement(ElementName = "SSP")]              public   string SSP                { get; set; }
        [XmlElement(ElementName = "SHE")]              public   string SHE                { get; set; }
        [XmlElement(ElementName = "SDI")]              public   string SDI                { get; set; }
        [XmlElement(ElementName = "SCL")]              public   string SCL                { get; set; }
        [XmlElement(ElementName = "ENT")]              public   string ENT                { get; set; }
        [XmlElement(ElementName = "ESP")]              public   string ESP                { get; set; }
        [XmlElement(ElementName = "EHE")]              public   string EHE                { get; set; }
        [XmlElement(ElementName = "EDI")]              public   string EDI                { get; set; }
        [XmlElement(ElementName = "ECL")]              public   string ECL                { get; set; }
        [XmlElement(ElementName = "WNT")]              public   string WNT                { get; set; }
        [XmlElement(ElementName = "WSP")]              public   string WSP                { get; set; }
        [XmlElement(ElementName = "WHE")]              public   string WHE                { get; set; }
        [XmlElement(ElementName = "WDI")]              public   string WDI                { get; set; }
        [XmlElement(ElementName = "WCL")]              public   string WCL                { get; set; }
     
        [XmlElement(ElementName = "IMPsNS")]             public string IMPsNS             { get; set; }
        [XmlElement(ElementName = "IMPsEW")]             public string IMPsEW             { get; set; }
        [XmlElement(ElementName = "IMPsNSAgainstDatum")] public string IMPsNSAgainstDatum { get; set; }
        [XmlElement(ElementName = "IMPsEWAgainstDatum")] public string IMPsEWAgainstDatum { get; set; }
        //
        [XmlElement(ElementName = "North")] public Cards North { get; set; }
        [XmlElement(ElementName = "East")] public Cards East { get; set; }
        [XmlElement(ElementName = "South")] public Cards South { get; set; }
        [XmlElement(ElementName = "West")] public Cards West { get; set; }


        public override bool Equals(object obj)
        {
            if (obj is Board other)
                return No == other.No;

            return false;
        }

        public bool Equals(Board other)
        {
            return No == other.No;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(No);
        }
    }
}
