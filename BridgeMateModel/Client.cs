using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class Client
    {
        [Column("ID", TypeName = "int")]    public int?   Id       { get; set; }
        [Column(TypeName = "varchar(127)")] public string Computer { get; set; }
    }
}
