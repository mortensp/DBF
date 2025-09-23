using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Data;

namespace DBF.Helpers
{
    public static class ExtentionMethods
    {
        public static int LineCount(this Group group)
        {
            return group.Records.Count() + 1 + (group.Groups?.Count ?? 0);
        }
    }
}
