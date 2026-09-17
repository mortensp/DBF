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
                var msg = (context ?? "").Trim() + " " + FormatException(ex);
                Log("EXC", msg);
            }

            catch { }
        }

        private static string FormatException(Exception ex)
        {
            if (ex == null)
                return "";

            var sb = new System.Text.StringBuilder();
            var current = ex;
            int depth = 0;

            while (current != null)
            {
                if (depth > 0)
                    sb.AppendLine();

                sb.Append($"{new string(' ', depth * 2)}[{current.GetType().Name}] {current.Message}");

                if (!string.IsNullOrEmpty(current.StackTrace))
                    sb.AppendLine().Append($"{new string(' ', depth * 2)}StackTrace: {current.StackTrace}");

                current = current.InnerException;
                depth++;
            }

            return sb.ToString();
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
