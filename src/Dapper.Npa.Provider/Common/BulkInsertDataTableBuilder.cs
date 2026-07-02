namespace Dapper.Npa.Provider.Common;

using System.Data;

internal static class BulkInsertDataTableBuilder
{
    public static DataTable Build(BulkInsertContext context)
    {
        var table = new DataTable();
        for (var i = 0; i < context.ColumnNames.Count; i++)
        {
            var columnType = context.ColumnTypes[i];
            var nullableType = Nullable.GetUnderlyingType(columnType) ?? columnType;
            table.Columns.Add(context.ColumnNames[i], nullableType);
        }

        foreach (var row in context.Rows)
        {
            var values = new object?[context.ColumnNames.Count];
            for (var i = 0; i < values.Length; i++)
                values[i] = row[i] ?? DBNull.Value;
            table.Rows.Add(values);
        }

        return table;
    }
}
