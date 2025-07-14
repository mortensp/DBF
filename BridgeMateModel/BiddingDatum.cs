using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class BiddingDatum
    {
        [Column("ID", TypeName = "int")] public  int?      Id        { get; set; }
        [Column(TypeName = "smallint")] public   short?    Section   { get; set; }
        [Column(TypeName = "smallint")] public   short?    Table     { get; set; }
        [Column(TypeName = "smallint")] public   short?    Round     { get; set; }
        [Column(TypeName = "smallint")] public   short?    Board     { get; set; }
        [Column(TypeName = "smallint")] public   short?    Counter   { get; set; }
        [Column(TypeName = "varchar(1)")] public string    Direction { get; set; }
        [Column(TypeName = "varchar(5)")] public string    Bid       { get; set; }
        [Column(TypeName = "datetime")] public   DateTime? DateLog   { get; set; }
        [Column(TypeName = "datetime")] public   DateTime? TimeLog   { get; set; }
        [Column(TypeName = "bool")] public       bool?     Erased    { get; set; }
        }
}
