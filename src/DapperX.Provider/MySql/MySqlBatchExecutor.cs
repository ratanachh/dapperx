namespace DapperX.Provider.MySql;

using DapperX.Core.Enums;
using DapperX.Provider.Common;
using MySqlConnector;

public sealed class MySqlBatchExecutor : IBulkInsertExecutor
{
    public static MySqlBatchExecutor Instance { get; } = new();

    public async Task InsertAsync(BulkInsertContext context, CancellationToken cancellationToken = default)
    {
        if (context.Connection is not MySqlConnection connection)
            throw new InvalidOperationException("MySQL bulk insert requires MySqlConnector.MySqlConnection.");

        var transaction = context.Transaction as MySqlTransaction;
        var table = BulkInsertDataTableBuilder.Build(context);
        var bulkCopy = new MySqlBulkCopy(connection, transaction)
        {
            DestinationTableName = context.TableName,
        };

        for (var i = 0; i < context.ColumnNames.Count; i++)
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, context.ColumnNames[i]));

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }
}
