//using BigBin;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;


namespace DBF.Helpers
{
    //[TraceOn()]
    public static class AccessInstaller
    {

        public static bool IsEngineInstalled
        {
            get
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\14.0\Access Connectivity Engine");
                return key != null;
            }
        }

        public static void CheckAndInstall()
        {
            if (!IsEngineInstalled)
            {
                var svar = MessageBox.Show("Access Database Engine 2010 (x86) er ikke installeret korrekt. Vil du gøre det nu, så programmet kan fortsætte? Det kan godt tage nogle minutter, men efter installation genstartes programmet"
                                          , "Installation"
                                          , MessageBoxButton.YesNo
                                          , MessageBoxImage.Information);

                if (svar == MessageBoxResult.No)
                    Environment.Exit(0); // Luk det nuværende program

                InstallEngine();
                RestartApplication();
            }
        }

        private static void InstallEngine()
        {
            try
            {
                string installerPath = @"InstallerFiles\AccessDatabaseEngine_2010_x86.exe"; // Justér sti efter behov

                var process = new Process();
                process.StartInfo.FileName = installerPath;
                process.StartInfo.Arguments = "/quiet /norestart /passive";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "runas"; // Kør som administrator
                process.Start();
                process.WaitForExit();
            }

            catch (Exception ex)
            {
                Debug.Write(ex.Message);
                throw;
            }
        }

        private static void RestartApplication()
        {
            //            string exePath = Assembly.GetExecutingAssembly().Location;
            string exePath = Process.GetCurrentProcess().MainModule.FileName;


            ProcessStartInfo startInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = true
                                           ,
                Verb = "runas" // Hvis du vil sikre administratorrettigheder igen
            };

            Process.Start(startInfo);
            Environment.Exit(0); // Luk det nuværende program
        }

    }
}