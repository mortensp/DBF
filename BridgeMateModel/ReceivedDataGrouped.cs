using System;
using System.Collections.Generic;

namespace DBF.BridgeMateModel
{
    public partial class ReceivedDataGrouped
    {
        public short? Section { get; set; }
        public short? Table { get; set; }
        public short? Round { get; set; }
        public short? Board { get; set; }
        public short? PairNs { get; set; }
        public short? PairEw { get; set; }
        public short? Declarer { get; set; }
        public string  NsEw { get; set; }
        public string  Contract { get; set; }
        public string  Result { get; set; }
        public string  LeadCard { get; set; }
        public string  Remarks { get; set; }
        public bool? Erased { get; set; }
    }
}
