using Dapper;
using Dapper.Npa.Abstractions.Logging;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Runtime.Configuration;
using Dapper.Npa.Runtime.Execution;
using Dapper.Npa.Runtime.Logging;
using Microsoft.Data.Sqlite;

namespace Dapper.Npa.Tests;

public class LoggingTests
{
    [Fact]
    public void TryLog_when_LogSql_false_does_not_invoke_logger()
    {
        var invoked = false;
        var options = new DapperXOptions
        {
            LogSql = false,
            Logger = _ => invoked = true,
        };

        SqlExecutionLogger.TryLog(
            DbExecutor.CreateLogContext("InsertAsync", options, DatabaseProvider.SqlServer),
            "INSERT INTO t VALUES (@id)",
            new { id = 1 });

        Assert.False(invoked);
    }

    [Fact]
    public void TryLog_when_LogSql_true_and_logger_null_does_not_throw()
    {
        var options = new DapperXOptions { LogSql = true, Logger = null };

        var ex = Record.Exception(() =>
            SqlExecutionLogger.TryLog(
                DbExecutor.CreateLogContext("InsertAsync", options, DatabaseProvider.SqlServer),
                "SELECT 1",
                null));

        Assert.Null(ex);
    }

    [Fact]
    public void TryLog_when_LogParameters_false_parameters_null()
    {
        DapperXLogEntry? captured = null;
        var options = new DapperXOptions
        {
            LogSql = true,
            LogParameters = false,
            Logger = e => captured = e,
        };

        SqlExecutionLogger.TryLog(
            DbExecutor.CreateLogContext("GetByIdAsync", options, DatabaseProvider.SqlServer),
            "SELECT * FROM t WHERE id = @id",
            new { id = 42 });

        Assert.NotNull(captured);
        Assert.Null(captured!.Parameters);
    }

    [Fact]
    public void TryLog_when_LogParameters_true_captures_anonymous_params_and_in_lists()
    {
        DapperXLogEntry? captured = null;
        var options = new DapperXOptions
        {
            LogSql = true,
            LogParameters = true,
            Logger = e => captured = e,
        };

        SqlExecutionLogger.TryLog(
            DbExecutor.CreateLogContext("FindAllByIdAsync", options, DatabaseProvider.SqlServer),
            "SELECT * FROM t WHERE id IN @ids",
            new { ids = new[] { 1, 2, 3 } });

        Assert.NotNull(captured?.Parameters);
        Assert.True(captured!.Parameters!.ContainsKey("ids"));
        Assert.Equal(new[] { 1, 2, 3 }, captured.Parameters["ids"]);
    }

    [Fact]
    public void TryLog_when_LogExecutableSql_false_executable_sql_null()
    {
        DapperXLogEntry? captured = null;
        var options = new DapperXOptions
        {
            LogSql = true,
            LogParameters = true,
            LogExecutableSql = false,
            Logger = e => captured = e,
        };

        SqlExecutionLogger.TryLog(
            DbExecutor.CreateLogContext("InsertAsync", options, DatabaseProvider.SqlServer),
            "INSERT INTO t VALUES (@id)",
            new { id = 1 });

        Assert.Null(captured!.ExecutableSql);
    }

    [Fact]
    public void TryLog_populates_method_name_from_context()
    {
        DapperXLogEntry? captured = null;
        var options = new DapperXOptions
        {
            LogSql = true,
            Logger = e => captured = e,
        };

        SqlExecutionLogger.TryLog(
            DbExecutor.CreateLogContext("UpsertManyAsync", options, DatabaseProvider.SqlServer),
            "MERGE t",
            null);

        Assert.Equal("UpsertManyAsync", captured!.MethodName);
    }

    [Fact]
    public async Task DbExecutor_logs_before_execution_not_after_success()
    {
        var phases = new List<string>();
        var options = new DapperXOptions
        {
            LogSql = true,
            Logger = _ => phases.Add("logged"),
        };

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE t (id INTEGER PRIMARY KEY);");

        await DbExecutor.ExecuteAsync(
            connection,
            "INSERT INTO t (id) VALUES (@id)",
            new { id = 1 },
            logContext: DbExecutor.CreateLogContext("TestMethod", options, DatabaseProvider.Sqlite));

        phases.Add("executed");
        Assert.Equal(new[] { "logged", "executed" }, phases);
    }
}
