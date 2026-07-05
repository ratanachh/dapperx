using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Logging;

namespace DapperX.Runtime.Configuration;

using System.Collections.Concurrent;
using DapperX.Abstractions.Configuration;
using DapperX.Abstractions.Logging;

/// <summary>
/// Runtime configuration for DapperX, registered via <c>AddDapperXRepositories</c> and injected into
/// generated repositories. Controls batch chunking, logging, and <c>[GlobalFilter]</c> activation.
/// </summary>
public sealed class DapperXOptions : IDapperXOptions
{
    /// <summary>Default chunk size for batch operations (<c>InsertManyAsync</c>, <c>UpdateManyAsync</c>, etc.) when no explicit <c>batchSize</c> is passed.</summary>
    public int BatchSize { get; set; } = 1000;
    /// <summary>Row-count threshold above which <c>InsertManyAsync</c> switches from chunked batch inserts to a provider-specific bulk insert.</summary>
    public int BulkThreshold { get; set; } = 5000;
    /// <summary>Callback invoked with a <see cref="DapperXLogEntry"/> for each executed statement, when <see cref="LogSql"/> is enabled.</summary>
    public Action<DapperXLogEntry>? Logger { get; set; }
    /// <summary>Enables invoking <see cref="Logger"/> with the SQL executed for each statement.</summary>
    public bool LogSql { get; set; }
    /// <summary>Includes bound parameter values in logged entries. Disabled by default to avoid leaking sensitive data into logs.</summary>
    public bool LogParameters { get; set; }
    /// <summary>Logs the SQL with parameter placeholders substituted with their literal values, for copy-paste debugging.</summary>
    public bool LogExecutableSql { get; set; }

    private readonly ConcurrentDictionary<string, object?> _activeFilters = new(StringComparer.Ordinal);

    /// <summary>Activates a <c>[GlobalFilter]</c> by name, optionally supplying parameters the filter's condition references.</summary>
    public void EnableFilter(string name, object? parameters = null)
        => _activeFilters[name] = parameters;

    /// <summary>Deactivates a previously enabled <c>[GlobalFilter]</c> by name.</summary>
    public void DisableFilter(string name)
        => _activeFilters.TryRemove(name, out _);

    /// <summary>Returns whether the named <c>[GlobalFilter]</c> is currently active.</summary>
    public bool IsFilterActive(string name)
        => _activeFilters.ContainsKey(name);

    /// <summary>Returns the parameters passed to <see cref="EnableFilter"/> for the named filter, or <c>null</c> if not set/active.</summary>
    public object? GetFilterParameters(string name)
        => _activeFilters.TryGetValue(name, out var p) ? p : null;
}
