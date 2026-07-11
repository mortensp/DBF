using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic.Logging;

namespace DBF.Helpers
{
    /// <summary>
    /// Minimal thread-safe file logger used by the application. Writes to a daily log file
    /// in the user's LocalApplicationData folder and also emits Debug output.
    /// </summary>
    public static class Logger
    {
        private static readonly object _sync      = new();
        private static readonly string _logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mortensp", "DBF", "logs");

        static Logger()
        {
            try
            {
                var clearLogsBefore = $"dbf_{DateTime.Now.AddDays(-10):yyyyMMdd}.log";
                Directory.CreateDirectory(_logFolder);

                foreach (var path in Directory.GetFiles(_logFolder, "dbf_????????.log"))
                {
#if RELEASE
                    if (string.Compare(Path.GetFileName(path), clearLogsBefore, StringComparison.Ordinal) <  0)
#endif
                        File.Delete(path);
                }
            }

            catch
            {
                // ignore
            }
        }

        public static string LogFilePath => Path.Combine(_logFolder, $"dbf_{DateTime.Now:yyyyMMdd}.log");

        public static void   Info(string message) => Log("INFO", message);

        [Conditional("DEBUG")]
        public static void Debug(string message) => Log("DEBUG", message);

        public static void Error(string message) => Log("ERROR", message);

        public static void Exception(Exception ex, string context = null)
        {
            try
            {
                var msg = (context ?? "").Trim() + " " + ex.ToString();
                Log("EXC", msg);
            }

            catch { }
        }

        private static void Log(string level, string message)
        {
            try
            {
                if (message == "")
                    level = "";

                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-6}] {message}" + Environment.NewLine;

                lock (_sync)
                {
                    File.AppendAllText(LogFilePath, line);
                }

                System.Diagnostics.Debug.WriteLine(line.TrimEnd());
            }

            catch
            {
                // avoid throwing from logger
            }
        }
    }
}
