using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBF.Helpers
{
    public class CrashDetector
    {
        private static readonly string CrashFilePath = 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mortensp\\DBF", "crashflag.json");

        public static bool DidCrashLastTime()
        {
            //File.WriteAllText("d:\\Crash.log", $"{CrashFilePath}: {File.Exists(CrashFilePath)}");
            return File.Exists(CrashFilePath);
        }

        public static void MarkAppStarted()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CrashFilePath)!);
            File.WriteAllText(CrashFilePath, "{ \"running\": true }");
        }

        public static void MarkAppExitedNormally()
        {
            if (File.Exists(CrashFilePath))
                File.Delete(CrashFilePath);
        }
    }
}
