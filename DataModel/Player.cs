using System;
using System.Xml.Serialization;
//using DeepCopy;

namespace DBF.DataModel
{
    [XmlRoot(ElementName = "Player")]     public class Player : IEquatable<Player>
    {
        //public Player()
        //{
            
        //}


        ////[DeepCopyConstructor]
        //public Player(Player other)
        //{

        //}
        public int     MemberNo           => MemberNoStr.AsInt();
        public int     Substitute         => SubstituteStr.AsInt();
        public decimal HAC                => HACStr.AsDecimal();
        public int     No                 => NoStr.AsInt();

        //-----
        [XmlElement(ElementName = "Name")]       public string Name          { get; set; }
        [XmlElement(ElementName = "MemberNo")]   public string MemberNoStr   { get; set; }
        [XmlElement(ElementName = "Substitute")] public string SubstituteStr { get; set; }
        [XmlElement(ElementName = "HAC")]        public string HACStr        { get; set; }
        [XmlAttribute(AttributeName = "No")]     public string NoStr         { get; set; }

        //-----
        public override string ToString() => Name;

        public string NameWithSubstitute  => Substitute == 0 ? Name : $"{Name} (S)";
        public string NameWithHac         => $"{Name} ({HAC})";

        public override bool Equals(object obj)
        {
            if (obj is Player other)
                return MemberNo == other.MemberNo;

            return false;
        }

        public bool Equals(Player other)
        {
            return MemberNo == other.MemberNo;
        }

        public override int GetHashCode()
        {
            // Brug de samme properties som i Equals
            return HashCode.Combine(MemberNo);
        }
    }
}