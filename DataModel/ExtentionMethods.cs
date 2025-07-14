using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.UI.Xaml.Schedule;

namespace DBF.DataModel
{
    public static class ExtentionMethods
    {
        public static double AsDouble(this string str, double defaultValue = 0)   => double.TryParse(str, CultureInfo.InvariantCulture, out double val) ? val : defaultValue;

        public static decimal AsDecimal(this string str, decimal defaultValue = 0) => decimal.TryParse(str, CultureInfo.InvariantCulture, out decimal val) ? val : defaultValue;
        public static int AsInt(this string str, int defaultValue = 0) => int.TryParse(str, out int val) ? val : defaultValue;
        
        public static bool AsBool(this string str, bool defaultValue = false)                => bool.TryParse(str, out bool val) ? val : defaultValue;

        static public TimeSpan Max(this TimeSpan t1, TimeSpan t2) => t1 >  t2 ? t1 : t2;

        static public TimeSpan Min(this TimeSpan t1, TimeSpan t2) => t1 <  t2 ? t1 : t2;

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
                    // Håndter collections
                    if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                    &&  prop.PropertyType != typeof(string))
                    {
                        var targetCollection = prop.GetValue(target) as IEnumerable;
                        var otherCollection  = value as IEnumerable;

                        // Hvis det er en generisk collection og kan tilføje elementer
                        var addMethod = prop.PropertyType.GetMethod("Add");

                        if (addMethod != null && targetCollection != null && otherCollection != null)
                        {
                            var targetList = targetCollection.Cast<object>().ToList();

                            foreach (var item in otherCollection)
                            {
                                var tItem = targetList.Find(i => i.Equals(item));

                                if (tItem is null)
                                    //if (!targetList.Contains(item))
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

                    // Hvis property er en kompleks type (og ikke string eller collection), kopier rekursivt
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
    }
}