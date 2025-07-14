using System;
using System.Collections.Generic;

namespace DBF.BridgeMateModel
{
    public partial class PlayDatum
    {
        public int? Id { get; set; }
        public short? Section { get; set; }
        public short? Table { get; set; }
        public short? Round { get; set; }
        public short? Board { get; set; }
        public short? Counter { get; set; }
        public string  Direction { get; set; }
        public string  Card { get; set; }
        public DateTime? DateLog { get; set; }
        public DateTime? TimeLog { get; set; }
        public bool? Erased { get; set; }
    }
}
