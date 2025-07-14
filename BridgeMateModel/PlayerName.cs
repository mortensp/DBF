using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class PlayerName
    {
        [Column("ID", TypeName = "int")] public           int    Id    { get; set; }
        [Column(TypeName = "varchar(9)")] public          string Name  { get; set; }
        [Column("strID", TypeName = "varchar(9)")] public string StrId { get; set; }

        public override string ToString() => $"{Id,6}: {Name}";
    }
}
