using System;
using System.Collections.Generic;

namespace DBF.BridgeMateModel
{
    public partial class HandEvaluation
    {
        public short? Section { get; set; }
        public short? Board { get; set; }
        public short? NorthSpades { get; set; }
        public short? NorthHearts { get; set; }
        public short? NorthDiamonds { get; set; }
        public short? NorthClubs { get; set; }
        public short? NorthNotrump { get; set; }
        public short? EastSpades { get; set; }
        public short? EastHearts { get; set; }
        public short? EastDiamonds { get; set; }
        public short? EastClubs { get; set; }
        public short? EastNotrump { get; set; }
        public short? SouthSpades { get; set; }
        public short? SouthHearts { get; set; }
        public short? SouthDiamonds { get; set; }
        public short? SouthClubs { get; set; }
        public short? SouthNotrump { get; set; }
        public short? WestSpades { get; set; }
        public short? WestHearts { get; set; }
        public short? WestDiamonds { get; set; }
        public short? WestClubs { get; set; }
        public short? WestNotrump { get; set; }
        public short? NorthHcp { get; set; }
        public short? EastHcp { get; set; }
        public short? SouthHcp { get; set; }
        public short? WestHcp { get; set; }
    }
}
