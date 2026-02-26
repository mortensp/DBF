using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBF.BridgeMateModel
{
    //[PrimaryKey(nameof(Id))]
    public partial class PlayerName
    {
        [Key]
        [Column("ID", TypeName = "int")] public           int    Id    { get; set; }
        [Column(TypeName = "varchar(9)")] public          string Name  { get; set; }
        [Column("strID", TypeName = "varchar(9)")] public string StrId { get; set; }

        public override string ToString() => $"{Id,6}: {Name}";
    }
}
