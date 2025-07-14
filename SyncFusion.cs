using Syncfusion.Licensing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace DBF
{
    public static class SyncFusion
    {
        public static string FindandRegisterLicenseKey() => FindLicenseKey();

        /// <summary>
        /// Helper method to find a syncfusion license key.
        /// </summary>
        /// <returns>License Key</returns>
        private static string FindLicenseKey()
        {
            int levelsToCheck = 25;

            // Get Current Syncfusion version
            var version            = Assembly.Load("Syncfusion.Licensing")?.GetName()?.Version;
            var major = $"V{version?.Major}:";

            // Get paths to search
            var filePath = @"SyncfusionLicense.txt";            
            var currentDirectory = Directory.GetParent(System.AppContext.BaseDirectory);
            
            StringBuilder searched = new StringBuilder();

            // Append strings to the StringBuilder
            searched.Append($"Searching for Syncfusion License Key for {major} in file: '{filePath}' starting at folder: '{currentDirectory?.FullName}' and up");

            if (version is not null
            && currentDirectory is not null
            && !string.IsNullOrEmpty(filePath))
            {
             

                for (int n = 0; n < levelsToCheck; n++)
                {
                    string fileDataPath = System.IO.Path.Combine(currentDirectory.FullName, filePath);

                    searched.Append($"{Environment.NewLine}  Directory: {currentDirectory.FullName}");

                    if (File.Exists(fileDataPath))
                    {
                        //Debug.WriteLine($"Syncfusion License file: {fileDataPath}");

                        foreach (var line in File.ReadLines(fileDataPath, Encoding.UTF8))
                        {
                            if (line.StartsWith(major, StringComparison.OrdinalIgnoreCase))
                            {
                                SyncfusionLicenseProvider.RegisterLicense(line.Replace(major, "", StringComparison.OrdinalIgnoreCase).Trim());
                                
                                return string.Empty;
                            }
                        }

                        searched.Append($" - Found, but no match.");
                    }

                    if (currentDirectory.Parent is null)
                        break;
                                    
                    currentDirectory = currentDirectory.Parent; 
                }
            }

            searched.Append($"{Environment.NewLine}License file is missing!");
            return searched.ToString();
            //return string.Empty;
        }
    }
}

