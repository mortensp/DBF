namespace DBF.Helpers
{
    public static class DBFMath
    {
        public static TimeSpan Max(TimeSpan t1, TimeSpan t2) => t1 >  t2 ? t1 : t2;

        public static TimeSpan Min(TimeSpan t1, TimeSpan t2) => t1 <  t2 ? t1 : t2;
    }
}
