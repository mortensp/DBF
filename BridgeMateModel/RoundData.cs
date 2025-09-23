using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    /// <summary>
    /// Hvem møder hvem ved hvilket bord og med hvilke spil
    /// </summary>
    public partial class RoundData
    {
        public short? Section { get; set; }
        public short? Table { get; set; }
        public short? Round { get; set; }
        public short? Nspair { get; set; }
        public short? Ewpair { get; set; }
        public short? LowBoard { get; set; }
        public short? HighBoard { get; set; }
        public string  CustomBoards { get; set; }

        
        [NotMapped] public int BoardsPlayed { get; set; }
        [NotMapped] public int BoardsPerRound => (HighBoard ?? 0) - (LowBoard ?? 0) + 1;
        [NotMapped] public bool Done => BoardsPlayed == BoardsPerRound;
    }
}
