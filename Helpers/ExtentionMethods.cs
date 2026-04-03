using System.Windows.Controls;
using System.Windows.Data;
using Syncfusion.Data;

using System;
using System.IO;
namespace DBF.Helpers;

public static class ExtentionMethods
{
    public static int LineCount(this Group group)
    {
        return group.Records.Count() + 1 + (group.Groups?.Count ?? 0);
    }

    public static DataGridBoundColumn FindColumn(this DataGrid grid, string name)
    {
        return grid.Columns
                   .OfType<DataGridBoundColumn>()
                   .FirstOrDefault(c => c.Binding is Binding b && b.Path.Path == name);
    }

    public static IEnumerable<DataGridBoundColumn> FindColumns(this DataGrid grid, params string[] names)
    {
        foreach (var col in grid.Columns.
            OfType<DataGridBoundColumn>().
            Where(c => c.Binding is Binding b && names.Contains(b.Path.Path)))
            yield return col;
    }

    public static string FindDeepestExistingDirectory(this string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Normaliser stien
        path = Path.GetFullPath(path);

        // Split i segmenter
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Start med roden (fx "C:\")
        string current = parts[0].EndsWith(":") ? parts[0] + Path.DirectorySeparatorChar : parts[0];

        string deepest = Directory.Exists(current) ? current : null;

        // Gå ned gennem stien
        for (int i = 1; i <  parts.Length; i++)
        {
            current = Path.Combine(current, parts[i]);

            if (Directory.Exists(current))
                deepest = current;
            else
                break;
        }

        return deepest;
    }

     public static string FirstNonSharedDirectory(this string fullPath,string basePath)
    {
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
        fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);

        var a = basePath.Split(Path.DirectorySeparatorChar);
        var b = fullPath.Split(Path.DirectorySeparatorChar);

        int i = 0;

        // Find første forskel
        while (i < a.Length && i < b.Length && 
               string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
        {
            i++;
        }

        // Hvis B er længere og der er en forskel → returnér første unikke segment
        if (i < b.Length)
            return b[i];

        return null; // B er ikke længere end A, eller de er identiske
    }

}

