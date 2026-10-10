//using Syncfusion.XlsIO.FormatParser;
namespace DBF.DataModel
{
    public class PageRange
    {
        public int  From                 { get; set; }
        public int  To                   { get; set; }

        public PageRange(int from, int to)
        {
            From = from;
            To   = to;
        }

        public bool IsEmpty => From >= To;

        //public bool Counted;

        public override string ToString() => $"{From}-{To}";

        internal        bool   Contains(int entryNo) => entryNo >= From && entryNo <  To;
    }
}

