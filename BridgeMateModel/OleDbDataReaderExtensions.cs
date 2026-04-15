using System.Data.OleDb;

namespace DBF.BridgeMateModel;

// Extension til OleDbDataReader for at tjekke om kolonnen findes
public static class OleDbDataReaderExtensions
{
    public static bool HasColumn(this OleDbDataReader reader, string columnName)
    {
        for (int i = 0; i <  reader.FieldCount; i++)
            if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}
