using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace DBF.BridgeMateModel
{
    [PrimaryKey(nameof(MainTournamentId), nameof(GroupTournamentId), nameof(SectionId))]
    public partial class Custom_DBF
    {
        [Column("MainTournamentID", TypeName = "int")] public  int? MainTournamentId  { get; set; }
        [Column("GroupTournamentID", TypeName = "int")] public int? GroupTournamentId { get; set; }
        [Column("SectionID", TypeName = "int")] public         int? SectionId         { get; set; }

        public override string ToString() => $"MainTournamentId:{MainTournamentId}, GroupTournamentId:{GroupTournamentId}, SectionId:{SectionId}";
    }
}
