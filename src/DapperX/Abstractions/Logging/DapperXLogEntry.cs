namespace DapperX.Abstractions.Logging;

public sealed class DapperXLogEntry
{
    /// <summary>Repository method name — compile-time string literal baked in by the generator.</summary>
    public string MethodName { get; init; } = string.Empty;

    /// <summary>SQL with @param placeholders. Always populated when logging is enabled.</summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>Parameter name→value pairs. Null unless LogParameters = true.</summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>SQL with values substituted inline (for copy-paste debugging). Null unless LogExecutableSql = true. Never used for execution.</summary>
    public string? ExecutableSql { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
