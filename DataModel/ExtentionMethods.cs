using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.DataModel
{
    public static class ExtentionMethods
    {
        public static double AsDouble(this string str, double defaultValue = 0)   => double.TryParse(str, CultureInfo.InvariantCulture, out double val) ? val : defaultValue;

        public static decimal AsDecimal(this string str, decimal defaultValue = 0)=> decimal.TryParse(str, CultureInfo.InvariantCulture, out decimal val) ? val : defaultValue;

        public static int AsInt(this string str, int defaultValue = 0)            => int.TryParse(str, out int val) ? val : defaultValue;

        public static bool AsBool(this string str, bool defaultValue = false)     => bool.TryParse(str, out bool val) ? val : defaultValue;

        static public TimeOnly Max(this TimeOnly t1, DateTime t2)                 => t1.Max(TimeOnly.FromDateTime(t2));

        static public TimeOnly Min(this TimeOnly t1, DateTime t2)                 => t1.Min(TimeOnly.FromDateTime(t2));

        static public TimeOnly Max(this TimeOnly t1, TimeOnly t2)                 => t1 >  t2 ? t1 : t2;

        static public TimeOnly Min(this TimeOnly t1, TimeOnly t2)                 => t1 <  t2 ? t1 : t2;

        static public TimeSpan Max(this TimeSpan t1, TimeSpan t2)                 => t1 >  t2 ? t1 : t2;

        static public TimeSpan Min(this TimeSpan t1, TimeSpan t2)                 => t1 <  t2 ? t1 : t2;

        static public DateTime Max(this DateTime t1, DateTime t2)                 => t1 >  t2 ? t1 : t2;

        static public DateTime Min(this DateTime t1, DateTime t2)                 => t1 <  t2 ? t1 : t2;

        public static void Merge<T>(this T target, T other)
        {
            if (other == null || target == null)
                throw new ArgumentNullException();

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in properties)
            {
                var value        = prop.GetValue(other);
                var defaultValue = prop.PropertyType.IsValueType
                                 ? Activator.CreateInstance(prop.PropertyType)
                                 : null;

                if (value != null && !value.Equals(defaultValue))
                {
                    // Handle collections
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                    &&  prop.PropertyType != typeof(string))
                    {
                        var targetCollection = prop.GetValue(target) as IEnumerable;
                        var otherCollection  = value as IEnumerable;

                        // If it is a generic collection and can add elements
                        var addMethod = prop.PropertyType.GetMethod("Add");

                        if (addMethod != null && targetCollection != null && otherCollection != null)
                        {
                            var targetList = targetCollection.Cast<object>().ToList();

                            foreach (var item in otherCollection)
                            {
                                var tItem = targetList.Find(i => i.Equals(item));

                                if (tItem is null)
                                    addMethod.Invoke(targetCollection, new[] { item });
                                else
                                {
                                    //   Merge(tItem, item);
                                    var mergeMethod  = typeof(ExtentionMethods)
                                        .GetMethod("Merge", BindingFlags.Public | BindingFlags.Static);
                                    var genericMerge = mergeMethod.MakeGenericMethod(item.GetType());
                                    genericMerge.Invoke(null, new object[] { tItem, item });
                                }
                            }
                        }

                        continue;
                    }

                    // If property is a complex type (and not string or collection), copy recursively
                    if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        var targetValue = prop.GetValue(target);

                        if (targetValue == null)
                        {
                            targetValue = Activator.CreateInstance(prop.PropertyType);
                            prop.SetValue(target, targetValue);
                        }

                        Merge(targetValue, value);
                    }
                    else
                        prop.SetValue(target, value);
                }
            }
        }

        public static string ToFriendlyString(this Enum value, string separator = ", ", string lastSeparator = " og ")
        {
            var type  = value.GetType();
            var flags = Enum.GetValues(type).Cast<Enum>()
                                            .Where(f => Convert.ToInt32(f) != 0 && value.HasFlag(f))
                                            .ToList();

            var names = flags.Select(f =>
                                     {
                                         var field = type.GetField(f.ToString());
                                         var attr  = field?.GetCustomAttribute<DescriptionAttribute>();
                                         return attr?.Description ?? f.ToString();
                                     }).ToList();

            if (names.Count == 0)
                return "";

            if (names.Count == 1)
                return names[0];

            // Set last separator as " and "
            return string.Join(separator, names.Take(names.Count - 1)) + lastSeparator + names.Last();
        }

        public static void RefreshSorting(this SfDataGrid sfDataGrid)
        {
            // Clear and reapply sorting
            var sortDescriptions = sfDataGrid.SortColumnDescriptions.ToList();
            var desc = sfDataGrid.SortColumnDescriptions;

            desc.Clear();
            
            foreach (var sortDesc in sortDescriptions)
            {
                desc.Add(sortDesc);
            }
        }
    }
}
