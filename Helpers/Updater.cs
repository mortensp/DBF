using System.Diagnostics;
using System.IO;
using DBF.Helpers;

namespace DBF.Helpers

{
    public class Updater
    {
        private static readonly string updaterPath  = $@"{AppContext.BaseDirectory}" + "Github Updater.exe";
        private static readonly string updaterPath2 = Directory.GetParent($"{AppContext.BaseDirectory}").Parent + @"\Github Updater\Github Updater.exe";
        private static readonly string updaterPath3 = $@"D:\Build\Github Updater\bin\{BuildInfo.Configuration}\Github Updater.exe";

        public static void Run(string mode ="latest")
        {
            var updater = new Process();

            updater.StartInfo.FileName = File.Exists(updaterPath)
                                       ? updaterPath
                                       : File.Exists(updaterPath2)
                                       ? updaterPath2
                                       : File.Exists(updaterPath3)
                                       ? updaterPath3
                                       : updaterPath;

            updater.StartInfo.Arguments = "repo:DBF owner:mortensp debug:true mode:"
                                        + 
                                        ( mode != "install"&& CrashDetector.DidCrashLastTime()
                                         ? "crashed"
                                         : mode);

            //File.WriteAllText("d:\\DBF.log", string.Join(" ",updater.StartInfo.FileName, updater.StartInfo.Arguments));
            if (File.Exists(updater.StartInfo.FileName))
            {
                updater.Start();
                updater.WaitForExit();

                if (updater.ExitCode == 1) // Update present and install has been started  
                {
                    CrashDetector.MarkAppExitedNormally();
                    Environment.Exit(0);
                }
            }
        }
    }
}
