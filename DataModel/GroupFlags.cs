namespace DBF.DataModel
{

    [Flags]
    public enum GroupFlags
    {
        None = 0,
        A = 1 << 0,
        B = 1 << 1,
        C = 1 << 2,
        D = 1 << 3
    }
}
