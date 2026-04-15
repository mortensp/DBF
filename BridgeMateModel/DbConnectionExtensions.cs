using System.Data;
using System.Data.Common;
using System.Data.Odbc;

namespace DBF.BridgeMateModel;

public static class DbConnectionExtensions
{
    public static List<string> GetDBColumns(this DbConnection connection, string table)
    {

        if (connection.State!=ConnectionState.Open)
            connection.Open();

        DataTable schema = connection.GetSchema("Columns", new string[] {null, null, table, null });

        return schema.Rows
                     .Cast<DataRow>()
                     .Select(r => r["COLUMN_NAME"].ToString())
                     .ToList();
    }
}
