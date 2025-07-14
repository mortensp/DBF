namespace DBF
{
    public static class DBFMath
    {
        static public TimeSpan Max( TimeSpan t1, TimeSpan t2) => t1 > t2 ? t1 : t2;
        static public TimeSpan Min( TimeSpan t1, TimeSpan t2) => t1 < t2 ? t1 : t2;
         
    }
}