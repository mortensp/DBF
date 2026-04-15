using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace DBF.BridgeMateModel;


[PrimaryKey(nameof(Id))]

public partial class IntermediateData
{
    public                                            int?      Id                 { get; set; }
    public                                            short?    Section            { get; set; }
    [Column("[Table]", TypeName = "smallint")] public short?    TableNo            { get; set; }
    public short? Round { get; set; }
    public short? Board { get; set; }
    public short? PairNs { get; set; }
    public short? PairEw { get; set; }
    public short? Declarer { get; set; }
    public                                            string    NsEw               { get; set; }
    public string Contract { get; set; }
    public string Result { get; set; }
    public string LeadCard { get; set; }
    public string Remarks { get; set; }
    public                                            DateTime? DateLog            { get; set; }
    public                                            DateTime? TimeLog            { get; set; }
    public bool? Processed { get; set; }
    public bool? Processed1 { get; set; }
    public bool? Processed2 { get; set; }
    public bool? Processed3 { get; set; }
    public bool? Processed4 { get; set; }
    public bool? Erased { get; set; }
    public bool? ExternalUpdate { get; set; }
    public short? SuspiciousContract { get; set; }

    override public string ToString()
    {
        return $"IntermediateData: Id:{Id}, Section:{Section}, TableNo:{TableNo}, Round:{Round}, Board:{Board}, PairNs:{PairNs}, PairEw:{PairEw}, Declarer:{Declarer}, NsEw:{NsEw}, Contract:{Contract}, Result:{Result}, LeadCard:{LeadCard}, Remarks:{Remarks}, DateLog:{DateLog}, TimeLog:{TimeLog}, Processed:{Processed}, Processed1:{Processed1}, Processed2:{Processed2}, Processed3:{Processed3}, Processed4:{Processed4}, Erased:{Erased}, ExternalUpdate:{ExternalUpdate}, SuspiciousContract:{SuspiciousContract}";
    }

}
