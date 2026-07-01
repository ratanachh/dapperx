namespace DapperX.Runtime.Configuration;

using System.Collections.Concurrent;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Logging;

public sealed class DapperXOptions : IDapperXOptions
{
    public int BatchSize { get; set; } = 1000;
    public int BulkThreshold { get; set; } = 5000;
    public Action<DapperXLogEntry>? Logger { get; set; }
    public bool LogSql { get; set; }
    public bool LogParameters { get; set; }
    public bool LogExecutableSql { get; set; }

    private readonly ConcurrentDictionary<string, object?> _activeFilters = new(StringComparer.Ordinal);

    public void EnableFilter(string name, object? parameters = null)
        => _activeFilters[name] = parameters;

    public void DisableFilter(string name)
        => _activeFilters.TryRemove(name, out _);

    public bool IsFilterActive(string name)
        => _activeFilters.ContainsKey(name);

    public object? GetFilterParameters(string name)
        => _activeFilters.TryGetValue(name, out var p) ? p : null;
}
