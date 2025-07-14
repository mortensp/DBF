using System;
using System.Collections.Generic;

namespace DBF.BridgeMateModel
{
    public partial class Setting
    {
        public short? Section { get; set; }
        public bool? ShowPairNumbers { get; set; }
        public bool? LeadCard { get; set; }
        public bool? MemberNumbers { get; set; }
        public bool? MemberNumbersNoBlankEntry { get; set; }
        public short? Bm2showPlayerNames { get; set; }
        public short? Bm2nameSource { get; set; }
        public bool? Bm2numberEntryEachRound { get; set; }
        public bool? Bm2numberEntryPreloadValues { get; set; }
    }
}
