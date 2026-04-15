using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
    
    [PrimaryKey(nameof(Id))]
    public partial class Client
    {

        [Column("ID", TypeName = "int")]    public int?   Id       { get; set; }
        [Column(TypeName = "varchar(127)")] public string Computer { get; set; }

        override public string ToString()
        {
            return $"Client: Id:{Id}, Computer:{Computer}";
        }
    }

}
