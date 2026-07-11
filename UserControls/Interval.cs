//using Syncfusion.XlsIO.FormatParser;

namespace DBF.UserControls
{
    public struct Interval
    {
        public int  From    { get; set; }
        public int  To      { get; set; }

        public Interval(int from, int to)
        {
            From = from;
            To   = to;
        }

        public bool IsEmpty                => From >= To;

        public override string ToString()  => $"{From}-{To}";

        internal bool Contains(int entryNo)=> entryNo >= From && entryNo <  To;
    }
}

