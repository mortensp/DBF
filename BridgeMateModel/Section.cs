using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;



using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
         [PrimaryKey(nameof(Id),nameof(Letter))]
    public partial class Section
    {
        [Column("ID", TypeName = "smallint")]               public short?  Id                       { get; set; }
        [Column(TypeName = "varchar(1)")]                   public string Letter                   { get; set; }
        [Column(TypeName = "smallint")]                     public short?  Tables                   { get; set; }
        [Column(TypeName = "smallint")]                     public short?  MissingPair              { get; set; }
        [Column("EWMoveBeforePlay", TypeName = "smallint")] public short?  EwmoveBeforePlay         { get; set; }
        [Column(TypeName = "smallint")]                     public short?  Session                  { get; set; }
        [Column(TypeName = "smallint")]                     public short?  ScoringType              { get; set; }
        [Column(TypeName = "int")]                          public int?    Winners                  { get; set; }
        [NotMapped]                                         public string OnlineEventGuid          { get; set; }
        [NotMapped]                                         public short?  OnlineEventRoundDuration { get; set; }

        // Navigation property - one Section has many RoundData
        public virtual ICollection<RoundData> Rounds { get; set; } = new List<RoundData>();

        override public string ToString() => $"Id:{Id}, Letter:{Letter}, Tables:{Tables}, MissingPair:{MissingPair}, EwmoveBeforePlay:{EwmoveBeforePlay}, Session:{Session}, ScoringType:{ScoringType}, Winners:{Winners}, OnlineEventGuid:{OnlineEventGuid}, OnlineEventRoundDuration:{OnlineEventRoundDuration}"; 
    }
}
