using System.Collections.Generic;
using System.Linq;

namespace AppArguments
{
    public static class ArgumentsExtensions
    {
        public static string ToFormattedString(this Dictionary<string, string> values)
        {
            if (values == null || values.Count == 0)
                return string.Empty;

            return string.Join(", ", values.Select(kv => kv.Key + ":" + kv.Value));
        }
    }
}
