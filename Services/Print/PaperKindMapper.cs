using System.Drawing.Printing;

namespace DBF.Services;

public static class PaperKindMapper
{
    private static readonly Dictionary<PaperKind, PageSize> _map = new()
    {
        // ISO A-series
        { PaperKind.A2, new PageSize("A2", 420, 594) },
        { PaperKind.A3, new PageSize("A3", 297, 420) },
        { PaperKind.A4, new PageSize("A4", 210, 297) },
        { PaperKind.A5, new PageSize("A5", 148, 210) },
        { PaperKind.A6, new PageSize("A6", 105, 148) },

        // US formats
        { PaperKind.Letter, new PageSize("Letter", 216, 279) },     // 8.5 × 11"
        { PaperKind.Legal, new PageSize("Legal", 216, 356) },      // 8.5 × 14"
        { PaperKind.Tabloid, new PageSize("Tabloid", 279, 432) },  // 11 × 17"
        { PaperKind.Executive, new PageSize("Executive", 184, 267) } // 7.25 × 10.5"
    };

    public static bool TryGet(PaperKind kind, out PageSize? size)
    {
        return _map.TryGetValue(kind, out size);
    }
}
