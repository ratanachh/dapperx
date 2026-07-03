using DapperX.Provider.Common;

namespace DapperX.Provider.PostgreSql;

using DapperX.Core.Enums;
using Provider.Common;
using Npgsql;
using NpgsqlTypes;

public sealed class PostgreSqlBulkExecutor : IBulkInsertExecutor
{
    public static PostgreSqlBulkExecutor Instance { get; } = new();

    public async Task InsertAsync(BulkInsertContext context, CancellationToken cancellationToken = default)
    {
        if (context.Connection is not NpgsqlConnection connection)
            throw new InvalidOperationException("PostgreSQL COPY requires NpgsqlConnection.");

        var columns = string.Join(", ", context.ColumnNames);
        var copyCommand = $"COPY {context.TableName} ({columns}) FROM STDIN (FORMAT BINARY)";
        await using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken);
        foreach (var row in context.Rows)
        {
            await writer.StartRowAsync(cancellationToken);
            for (var i = 0; i < context.ColumnNames.Count; i++)
                await WriteValueAsync(writer, context.ColumnTypes[i], row[i], cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
    }

    private static async Task WriteValueAsync(
        NpgsqlBinaryImporter writer,
        Type columnType,
        object? value,
        CancellationToken cancellationToken)
    {
        if (value is null)
        {
            await writer.WriteNullAsync(cancellationToken);
            return;
        }

        var underlying = Nullable.GetUnderlyingType(columnType) ?? columnType;
        if (underlying == typeof(int))
        {
            await writer.WriteAsync(Convert.ToInt32(value), NpgsqlDbType.Integer, cancellationToken);
            return;
        }

        if (underlying == typeof(long))
        {
            await writer.WriteAsync(Convert.ToInt64(value), NpgsqlDbType.Bigint, cancellationToken);
            return;
        }

        if (underlying == typeof(string))
        {
            await writer.WriteAsync(Convert.ToString(value), NpgsqlDbType.Text, cancellationToken);
            return;
        }

        if (underlying == typeof(bool))
        {
            await writer.WriteAsync(Convert.ToBoolean(value), NpgsqlDbType.Boolean, cancellationToken);
            return;
        }

        if (underlying == typeof(decimal))
        {
            await writer.WriteAsync(Convert.ToDecimal(value), NpgsqlDbType.Numeric, cancellationToken);
            return;
        }

        if (underlying == typeof(DateTime))
        {
            await writer.WriteAsync(Convert.ToDateTime(value), NpgsqlDbType.Timestamp, cancellationToken);
            return;
        }

        if (underlying == typeof(DateTimeOffset))
        {
            await writer.WriteAsync((DateTimeOffset)value, NpgsqlDbType.TimestampTz, cancellationToken);
            return;
        }

        if (underlying == typeof(Guid))
        {
            await writer.WriteAsync((Guid)value, NpgsqlDbType.Uuid, cancellationToken);
            return;
        }

        await writer.WriteAsync(value, cancellationToken);
    }
}
