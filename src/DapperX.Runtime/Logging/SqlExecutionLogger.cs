namespace DapperX.Runtime.Logging;

using DapperX.Abstractions.Logging;
using DapperX.Core.Enums;
using DapperX.Runtime.Execution;

/// <summary>Builds and invokes <see cref="DapperXLogEntry"/> before Dapper execution.</summary>
public static class SqlExecutionLogger
{
    public static void TryLog(
        in DbExecutionLogContext context,
        string sql,
        object? param)
    {
        var options = context.Options;
        if (options is null || !options.LogSql || options.Logger is null)
            return;

        IReadOnlyDictionary<string, object?>? parameters = null;
        string? executableSql = null;

        if (options.LogParameters || options.LogExecutableSql)
        {
            var extracted = ParameterExtractor.Extract(param);
            if (options.LogParameters)
                parameters = extracted;

            if (options.LogExecutableSql && extracted is not null)
                executableSql = ExecutableSqlFormatter.Format(sql, extracted, context.Provider);
        }

        options.Logger(new DapperXLogEntry
        {
            MethodName = context.MethodName ?? string.Empty,
            Sql = sql,
            Parameters = parameters,
            ExecutableSql = executableSql,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>Logs batch/graph sizing metadata before chunked execution (SQL Visibility &amp; Tracing).</summary>
    public static void TryLogBatchTrace(
        in DbExecutionLogContext context,
        string sql,
        int entityCount,
        int batchSize)
    {
        var options = context.Options;
        if (options is null || !options.LogSql || options.Logger is null)
            return;

        IReadOnlyDictionary<string, object?>? parameters = null;
        if (options.LogParameters)
        {
            parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["entityCount"] = entityCount,
                ["batchSize"] = batchSize,
            };
        }

        options.Logger(new DapperXLogEntry
        {
            MethodName = context.MethodName ?? string.Empty,
            Sql = sql,
            Parameters = parameters,
            ExecutableSql = null,
            Timestamp = DateTime.UtcNow,
        });
    }
}
