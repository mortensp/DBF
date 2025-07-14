using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class PlayerNumber
    {
        [Column(TypeName = "smallint")] public   short?    Section   { get; set; }
        [Column(TypeName = "smallint")] public   short?    Table     { get; set; }
        [Column(TypeName = "varchar(1)")] public string    Direction { get; set; }
        [Column(TypeName = "varchar(8)")] public string    Number    { get; set; }
        [Column(TypeName = "varchar(9)")] public string    Name      { get; set; }
        [Column(TypeName = "bool")] public       bool?     Updated   { get; set; }
        [Column(TypeName = "datetime")] public   DateTime? TimeLog   { get; set; }
        [Column(TypeName = "bool")] public       bool?     Processed { get; set; }
        [Column(TypeName = "smallint")] public   short?    Round     { get; set; }
    }
}
