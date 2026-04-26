using System.Xml;
using System.Xml.Serialization;

namespace DBF.DataModel
{
    /// <summary>
    /// A GroupTournament corresponds to a Group - e.g. Group B
    /// This contains, among other things, a description and filename. The file it points to contains
    /// a Tournament, which is a further description of the section on the given play day
    /// </summary>
    [XmlRoot(ElementName = "GroupTournament")]     
    public class GroupTournament : IEquatable<GroupTournament>
    {
        public int LastCompletedRound => LastCompletedRoundStr.AsInt();

        //-----
        [XmlElement(ElementName = "TournamentType")]     public string  TournamentType        { get; set; }
        [XmlElement(ElementName = "Description")]        public string  Description           { get; set; }
        [XmlElement(ElementName = "Section")]            public Section Section               { get; set; }
        [XmlElement(ElementName = "LastCompletedRound")] public string  LastCompletedRoundStr { get; set; }
        [XmlElement(ElementName = "SectionCompleted")]   public string  SectionCompleted      { get; set; }
        [XmlElement(ElementName = "Filename")]           public string  FileName              { get; set; }
        [XmlAttribute(AttributeName = "Id")]             public string  Id                    { get; set; }
        [XmlAttribute(AttributeName = "GroupName")]      public string  GroupName             { get; set; }

        //-----
        public override bool Equals(object obj)
        {
            if (obj is GroupTournament other)
                return Id == other.Id;

            return false;
        }

        public bool Equals(GroupTournament other)
        {
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            // Use the same properties as in Equals
            return HashCode.Combine(Id);
        }

        public override string ToString() => $"{FileName}: {GroupName}";
    }
}
