using System.Windows.Controls;
using System.Windows.Data;
using Syncfusion.Data;

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
    }

