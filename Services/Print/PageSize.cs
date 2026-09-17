using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.Services;

public record PageSize(string Name, double WidthMm, double HeightMm);

public static class PageSizes
{
    // ISO A-series
    public static readonly PageSize A0  = new("A0", 841, 1189);
    public static readonly PageSize A1  = new("A1", 594, 841);
    public static readonly PageSize A2  = new("A2", 420, 594);
    public static readonly PageSize A3  = new("A3", 297, 420);
    public static readonly PageSize A4  = new("A4", 210, 297);
    public static readonly PageSize A5  = new("A5", 148, 210);
    public static readonly PageSize A6  = new("A6", 105, 148);
    public static readonly PageSize A7  = new("A7", 74, 105);
    public static readonly PageSize A8  = new("A8", 52, 74);
    public static readonly PageSize A9  = new("A9", 37, 52);
    public static readonly PageSize A10 = new("A10", 26, 37);

    // US formats (converted from inches → mm)
    public static readonly PageSize Letter    = new("Letter", 216, 279);      // 8.5 × 11"
    public static readonly PageSize Legal     = new("Legal", 216, 356);       // 8.5 × 14"
    public static readonly PageSize Tabloid   = new("Tabloid", 279, 432);   // 11 × 17"
    public static readonly PageSize Executive = new("Executive", 184, 267); // 7.25 × 10.5"

    public static IEnumerable<PageSize> All
    {
        get
        {
            yield return A0;
            yield return A1;
            yield return A2;
            yield return A3;
            yield return A4;
            yield return A5;
            yield return A6;
            yield return A7;
            yield return A8;
            yield return A9;
            yield return A10;

            yield return Letter;
            yield return Legal;
            yield return Tabloid;
            yield return Executive;
        }
    }

    public static PageSize ConvertPaperSize(PaperSize ps)
    {
        double widthMm  = ps.Width * 0.254;   // 1 hundredth inch = 0.254 mm
        double heightMm = ps.Height * 0.254;

        return new PageSize(ps.PaperName, widthMm, heightMm);
    }

    public static PageSize ResolvePageSize(PaperSize ps)
    {
        // 1: Try PaperKind → PageSize mapping
        if (PaperKindMapper.TryGet(ps.Kind, out var mapped))
            return mapped!;

        // 2: Try to convert printer's size
        if (ps.Width >  0 && ps.Height >  0)
            return ConvertPaperSize(ps);

        // 3: Fallback
        return PageSizes.A4;
    }
}
