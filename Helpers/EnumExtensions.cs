using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DBF.Helpers
{
    public static class EnumExtensions
    {
        public static List<string> GetFlagNames<T>(this T flags, bool includeZeroName = false) where T : Enum
        {
            var  type       = typeof(T);
            var  flagValues = Enum.GetValues(type).Cast<T>();
            long flagsValue = Convert.ToInt64(flags);

            if (flagsValue == 0)
            {
                if (includeZeroName)
                {
                    var zero = flagValues.FirstOrDefault(v => Convert.ToInt64(v) == 0);
                    return zero is null ? new List<string>() : new List<string> { zero.ToString() };
                }

                return new List<string>();
            }

            var result = new List<string>();

            foreach (var v in flagValues)
            {
                long val = Convert.ToInt64(v);

                if (val == 0) // skip zero-valued flag for non-zero input
                    continue;

                if ((flagsValue & val) == val)
                    result.Add(v.ToString());
            }

            return result;
        }

        // Returns the enum values that are set in the flags (e.g., [GroupFlags.A, GroupFlags.C])
        public static List<T> GetFlagValues<T>(this T flags, bool includeZero = false) where T : Enum
        {
            var  type       = typeof(T);
            var  flagValues = Enum.GetValues(type).Cast<T>();
            long flagsValue = Convert.ToInt64(flags);

            if (flagsValue == 0)
            {
                if (includeZero)
                {
                    var zero = flagValues.FirstOrDefault(v => Convert.ToInt64(v) == 0);
                    return zero is null ? new List<T>() : new List<T> { zero };
                }

                return new List<T>();
            }

            var result = new List<T>();

            foreach (var v in flagValues)
            {
                long val = Convert.ToInt64(v);

                if (val == 0) // skip zero-valued flag for non-zero input
                    continue;

                if ((flagsValue & val) == val)
                    result.Add(v);
            }

            return result;
        }

        // Returns the numeric (underlying) values as long (e.g., [1,4])
        public static List<long> GetFlagNumericValues<T>(this T flags, bool includeZero = false) where T : Enum
        {
            return flags.GetFlagValues(includeZero).Select(v => Convert.ToInt64(v)).ToList();
        }

        [DebuggerStepThrough]
        public static bool Contains<T>(this T item, T others) where T : Enum, IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type", typeof(T).Name);

            var itm = (int)(IConvertible)item;
            var oth = (int)(IConvertible)others;

            if (Attribute.IsDefined(typeof(T), typeof(FlagsAttribute))
            && (itm == 0 || oth == 0))
                return false;

            return (itm & oth) == oth;
        }
    }
}
