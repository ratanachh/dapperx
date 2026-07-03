using DapperX.Provider.Common;

namespace DapperX.Provider.SqlServer;

using DapperX.Core.Enums;
using Provider.Common;
using Microsoft.Data.SqlClient;

public sealed class SqlServerBulkExecutor : IBulkInsertExecutor
{
    public static SqlServerBulkExecutor Instance { get; } = new();

    public Task InsertAsync(BulkInsertContext context, CancellationToken cancellationToken = default)
    {
        if (context.Connection is not SqlConnection sqlConnection)
            throw new InvalidOperationException("SqlBulkCopy requires Microsoft.Data.SqlClient.SqlConnection.");

        var transaction = context.Transaction as SqlTransaction;
        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = context.TableName,
        };

        for (var i = 0; i < context.ColumnNames.Count; i++)
            bulkCopy.ColumnMappings.Add(context.ColumnNames[i], context.ColumnNames[i]);

        var table = BulkInsertDataTableBuilder.Build(context);
        bulkCopy.WriteToServer(table);
        return Task.CompletedTask;
    }
}
