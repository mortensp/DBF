using System;
using System.IO;

namespace DBF
{
    class TextReplacer
    {
        public static void FindAndReplaceInFile(string filePath, string searchText, string replaceText)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Filen findes ikke.");
                return;
            }

            try
            {
                string content        = File.ReadAllText(filePath);
                string updatedContent = content.Replace(searchText, replaceText);
                File.WriteAllText(filePath, updatedContent);
                Console.WriteLine("Erstatning fuldført.");
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Fejl under behandling: {ex.Message}");
            }
        }
    }
}
