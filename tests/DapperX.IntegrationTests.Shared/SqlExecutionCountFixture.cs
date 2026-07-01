using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Logging;
using DapperX.Runtime.Configuration;

namespace DapperX.IntegrationTests.Shared;

/// <summary>Counts SQL executions via <see cref="DapperXLogEntry"/> when <see cref="IDapperXOptions.LogSql"/> is enabled.</summary>
public sealed class SqlExecutionCountFixture
{
    private readonly List<DapperXLogEntry> _entries = [];

    public IDapperXOptions Options { get; }

    public SqlExecutionCountFixture(bool logParameters = false, bool logExecutableSql = false)
    {
        Options = new DapperXOptions
        {
            LogSql = true,
            LogParameters = logParameters,
            LogExecutableSql = logExecutableSql,
            Logger = entry => _entries.Add(entry),
        };
    }

    public int SqlCallCount => _entries.Count;

    public IReadOnlyList<DapperXLogEntry> Entries => _entries;

    public void Reset() => _entries.Clear();

    public void AssertSqlCallCount(int expected)
    {
        Assert.Equal(expected, SqlCallCount);
    }
}
