using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Group = Syncfusion.Data.Group;
namespace DBF.Helpers;

public static class ExtentionMethods
{
    public static bool WildcardMatch(this string input, string pattern)
    {
        string regex = "^" + Regex.Escape(pattern)
                                  .Replace("\\*", ".*")
                                  .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }
    public static int LineCount(this Group group)
    {
        return (group.Records?.Count() ?? 0) + 1 + (group.Groups?.Count ?? 0);
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

        // Normalize path    
        path = Path.GetFullPath(path);

        // Split into segments
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Begin at the root (fx "C:\")
        string current = parts[0].EndsWith(":") ? parts[0] + Path.DirectorySeparatorChar : parts[0];

        string deepest = Directory.Exists(current) ? current : null;

        // Parse and go down the path
        for (int i = 1; i < parts.Length; i++)
        {
            current = Path.Combine(current, parts[i]);

            if (Directory.Exists(current))
                deepest = current;
            else
                break;
        }

        return deepest;
    }

    public static string FirstNonSharedDirectory(this string fullPath, string basePath)
    {
        basePath = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar);
        fullPath = Path.GetFullPath(fullPath).TrimEnd(Path.DirectorySeparatorChar);

        var a = basePath.Split(Path.DirectorySeparatorChar);
        var b = fullPath.Split(Path.DirectorySeparatorChar);

        int i = 0;

        // Find first difference
        while (i < a.Length && i < b.Length &&
               string.Equals(a[i], b[i], StringComparison.OrdinalIgnoreCase))
        {
            i++;
        }

        // If B is longer and there is a difference → return the first unique segment
        if (i < b.Length)
            return b[i];

        return null; // B is not longer than A, or they are identical
    }

    public static string GetLeafDirectoryName(this string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        // Trim any trailing separators
        var trimmed = path.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        try
        {
            // If the path points to an existing directory, return its last segment
            if (Directory.Exists(trimmed))
                return System.IO.Path.GetFileName(trimmed) ?? string.Empty;

            // Otherwise assume it's a file (or non-existing path) and return the containing folder's last segment
            var dir = System.IO.Path.GetDirectoryName(trimmed);

            if (string.IsNullOrEmpty(dir))
                return string.Empty;

            return System.IO.Path.GetFileName(dir.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)) ?? string.Empty;
        }

        catch
        {
            return string.Empty;
        }
    }

    public static bool IsUsableDirectory(this string path)
    {
        var dirInfo = new DirectoryInfo(path);

        if (!dirInfo.Exists)
            return false;

        if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            try
            {
                Directory.GetFiles(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }



    public static bool IsDirectoryLink(this string path)
    {
        var dirInfo = new DirectoryInfo(path);


        if (!dirInfo.Exists)
            return false;

        return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }



}

