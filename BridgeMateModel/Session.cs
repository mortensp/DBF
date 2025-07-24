using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    public partial class Session
    {
        public DateTime Date => date is DateTime d && time is DateTime t ? new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute, t.Second) : default;

        //---
        [Column("ID", TypeName = "smallint")]      public short?    Id        { get; set; }
        [Column(TypeName = "varchar(20)")]         public string   Name      { get; set; }
        [Column("date", TypeName = "datetime")]    public DateTime? date      { get; set; }
        [Column("time", TypeName = "datetime")]    public DateTime? time      { get; set; }
        [Column("GUID", TypeName = "varchar(40)")] public string   Guid      { get; set; }
        [Column(TypeName = "smallint")]            public short?    Status    { get; set; }
        [Column(TypeName = "bool")]                public bool?     ShowInApp { get; set; }
    }
}
