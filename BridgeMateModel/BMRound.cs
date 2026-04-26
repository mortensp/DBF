using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
    /// <summary>
    /// Hvem møder hvem ved hvilket bord og med hvilke spil
    /// </summary>
    [PrimaryKey(nameof(Section), nameof(Round), nameof(TableNo))]
    [Table("RoundData")]
    public partial class BMRound
    {
        public                                          short?  Section         { get; set; }
        [Column("Table", TypeName = "smallint")] public short?  TableNo         { get; set; }
        public                                          short?  Round           { get; set; }
        public                                          short?  Nspair          { get; set; }
        public                                          short?  Ewpair          { get; set; }
        public                                          short?  LowBoard        { get; set; }
        public                                          short?  HighBoard       { get; set; }
        public                                          string  CustomBoards    { get; set; }

        [NotMapped] public                              int     BoardsPlayed    { get; set; }
        [NotMapped] public int  BoardsPerRound  => (HighBoard ?? 0) - (LowBoard ?? 0) + 1;
        [NotMapped] public int  BoardsRemaining => BoardsPerRound - BoardsPlayed;
        [NotMapped] public bool Done            => BoardsPlayed == BoardsPerRound;

        // Navigation property to Section (1:n)
        [ForeignKey(nameof(Section))]
        public virtual                                  Section SectionEntity   { get; set; }
             //public override string  ToString() => $"Section:{Section}, TableNo:{TableNo}, Round:{Round}, Nspair:{Nspair}, Ewpair:{Ewpair}, LowBoard:{LowBoard}, HighBoard:{HighBoard}, CustomBoards:{CustomBoards}, BoardsPlayed:{BoardsPlayed}";
   
        public override string  ToString() => Done
            ? $"Section: {Section}, TableNo: {TableNo}, Round: {Round}, Done"
            :$"Section: {Section}, TableNo: {TableNo}, Round: {Round}, Board: {BoardsPlayed} of {BoardsPerRound}";
    }
}
