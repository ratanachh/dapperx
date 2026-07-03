using Dapper;
using DapperX.Core.Enums;
using DapperX.Runtime.Configuration;
using DapperX.Runtime.Execution;
using DapperX.Runtime.Logging;
using Microsoft.Data.Sqlite;

namespace DapperX.Tests;

public class LoggingIntegrationTests
{
    [Fact]
    public async Task ExecutableSql_from_logger_executes_on_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT NOT NULL);");

        string? executable = null;
        var options = new DapperXOptions
        {
            LogSql = true,
            LogParameters = true,
            LogExecutableSql = true,
            Logger = e => executable = e.ExecutableSql,
        };

        await DbExecutor.ExecuteAsync(
            connection,
            "INSERT INTO items (id, name) VALUES (@id, @name)",
            new { id = 1, name = "alpha" },
            logContext: DbExecutor.CreateLogContext("InsertAsync", options, DatabaseProvider.Sqlite));

        Assert.NotNull(executable);
        Assert.Equal(
            "INSERT INTO items (id, name) VALUES (1, 'alpha')",
            executable);

        var selectSql = ExecutableSqlFormatter.Format(
            "SELECT name FROM items WHERE id = @id",
            new Dictionary<string, object?> { ["id"] = 1 },
            DatabaseProvider.Sqlite);
        var name = await connection.ExecuteScalarAsync<string>(selectSql);
        Assert.Equal("alpha", name);
    }

    [Fact]
    public void ExecutableSql_bool_formats_for_sqlite_provider()
    {
        var sql = ExecutableSqlFormatter.Format(
            "UPDATE t SET active = @active WHERE id = @id",
            new Dictionary<string, object?> { ["active"] = true, ["id"] = 1 },
            DatabaseProvider.Sqlite);

        Assert.Equal("UPDATE t SET active = 1 WHERE id = 1", sql);
    }
}
