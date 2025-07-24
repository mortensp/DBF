using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
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
    }
}
