using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
    /// <summary>
    /// Indeholder en rækker for hver ComputerId og for hvert bord i hver Section og Group
    /// </summary>
    [PrimaryKey(nameof(Section), nameof(TableNo), nameof(ComputerId))]
    public partial class Table
    {
        [Column(TypeName = "smallint")] public            short? ComputerId      { get; set; }
        [Column(TypeName = "smallint")] public            short? Section         { get; set; }
        [Column(TypeName = "smallint")] public            short? Group           { get; set; }
        [Column("Table", TypeName = "smallint")] public short? TableNo         { get; set; }
        //---
        [Column(TypeName = "smallint")] public            short? Status          { get; set; }
        [Column(TypeName = "smallint")] public            short? LogOnOff        { get; set; }
        [Column(TypeName = "smallint")] public            short? CurrentRound    { get; set; }
        [Column(TypeName = "smallint")] public            short? CurrentBoard    { get; set; }
        [Column(TypeName = "smallint")] public            short? UpdateFromRound { get; set; }

        public override string ToString()
        {
            return $"Section: {Section}, Table: {TableNo}, ComputerId: {ComputerId}";
        }
    }
}
