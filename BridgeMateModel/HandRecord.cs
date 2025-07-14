using System;
using System.Collections.Generic;

namespace DBF.BridgeMateModel
{
    public partial class HandRecord
    {
        public short? Section { get; set; }
        public short? Board { get; set; }
        public string  NorthSpades { get; set; }
        public string  NorthHearts { get; set; }
        public string  NorthDiamonds { get; set; }
        public string  NorthClubs { get; set; }
        public string  EastSpades { get; set; }
        public string  EastHearts { get; set; }
        public string  EastDiamonds { get; set; }
        public string  EastClubs { get; set; }
        public string  SouthSpades { get; set; }
        public string  SouthHearts { get; set; }
        public string  SouthDiamonds { get; set; }
        public string  SouthClubs { get; set; }
        public string  WestSpades { get; set; }
        public string  WestHearts { get; set; }
        public string  WestDiamonds { get; set; }
        public string  WestClubs { get; set; }
    }
}
