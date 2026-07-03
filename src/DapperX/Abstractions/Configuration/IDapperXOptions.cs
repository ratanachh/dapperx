using DapperX.Abstractions.Logging;

namespace DapperX.Abstractions.Configuration;

using Abstractions.Logging;

public interface IDapperXOptions
{
    int BatchSize { get; }
    int BulkThreshold { get; }
    Action<DapperXLogEntry>? Logger { get; }
    bool LogSql { get; }
    bool LogParameters { get; }
    bool LogExecutableSql { get; }

    // Global filter methods — scoped per DI instance, never static
    void EnableFilter(string name, object? parameters = null);
    void DisableFilter(string name);
    bool IsFilterActive(string name);
    object? GetFilterParameters(string name);
}
