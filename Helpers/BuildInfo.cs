using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.Helpers
{
    public static class BuildInfo
    {
#if DEBUG
        public const string Configuration = "Debug";
#else
    public const string Configuration = "Release";
#endif
    }
}

