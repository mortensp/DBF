using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
    [PrimaryKey(nameof(Section), nameof(TableNo), nameof(Direction))]
    public partial class PlayerNumber
    {
        [Column(TypeName = "smallint")] public          short?    Section   { get; set; }
        [Column("Table", TypeName = "smallint")] public short?    TableNo   { get; set; }
        [Column(TypeName = "varchar(1)")] public        string    Direction { get; set; }
        [Column(TypeName = "varchar(8)")] public        string    Number    { get; set; }
        [Column(TypeName = "varchar(9)")] public        string    Name      { get; set; }
        [Column(TypeName = "bool")] public              bool?     Updated   { get; set; }
        [Column(TypeName = "datetime")] public          DateTime? TimeLog   { get; set; }
        [Column(TypeName = "bool")] public              bool?     Processed { get; set; }
        [Column(TypeName = "smallint")] public          short?    Round     { get; set; }

        override public string ToString() => $"Section:{Section}, TableNo:{TableNo}, Direction:{Direction}, Number:{Number}, Name:{Name}, Updated:{Updated}, TimeLog:{TimeLog}, Processed:{Processed}, Round:{Round}";
    }
}
