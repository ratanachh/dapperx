using DapperX.Core.Enums;

namespace DapperX.Runtime.Query;

/// <summary>Repository-specific settings for runtime <see cref="RepositoryQuery{T}"/>.</summary>
public sealed class QueryRuntimeConfig
{
    public bool SoftDeleteSupported { get; init; }
    public string? SoftDeleteColumn { get; init; }
    public string? TenantIdColumn { get; init; }
    public IReadOnlyDictionary<string, string> ProjectionBaseSql { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
    public Func<string, string>? ApplyGlobalFilters { get; init; }
    public IReadOnlyList<string> GlobalFilterNames { get; init; } = Array.Empty<string>();
    public Func<object?>? GetTenantId { get; init; }
    public string Provider { get; init; } = "SqlServer";
    public IReadOnlyDictionary<string, string> IncludeJoinSql { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);
    public string MainAlias { get; init; } = "e";
}

internal static class QueryLockSuffix
{
    public static string Get(LockMode mode, string provider, int timeoutMs)
    {
        if (mode is LockMode.Optimistic)
            return string.Empty;

        if (provider == "Sqlite")
            throw new NotSupportedException("Pessimistic lock is not supported on SQLite.");

        return mode switch
        {
            LockMode.Pessimistic => provider switch
            {
                "PostgreSql" => timeoutMs == 0 ? " FOR UPDATE NOWAIT" : " FOR UPDATE",
                "MySql" => " FOR UPDATE NOWAIT",
                _ => " WITH (UPDLOCK, ROWLOCK)",
            },
            LockMode.PessimisticRead => provider switch
            {
                "PostgreSql" => timeoutMs == 0 ? " FOR SHARE NOWAIT" : " FOR SHARE",
                "MySql" => " FOR SHARE",
                _ => " WITH (HOLDLOCK, ROWLOCK)",
            },
            _ => string.Empty,
        };
    }

    public static string? GetPreamble(LockMode mode, string provider, int timeoutMs)
    {
        if (mode is LockMode.Optimistic || timeoutMs <= 0)
            return null;

        return provider switch
        {
            "SqlServer" => "SET LOCK_TIMEOUT @lockTimeoutMs; ",
            "PostgreSql" => "SET lock_timeout = @lockTimeoutMs; ",
            _ => null,
        };
    }
}
