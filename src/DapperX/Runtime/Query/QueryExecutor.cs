using DapperX.Core.Enums;
using DapperX.Query.Expressions;
using DapperX.Query.Query;

namespace DapperX.Runtime.Query;

/// <summary>Builds SQL from compile-time base SELECT + runtime WHERE/ORDER BY (Requirements Pattern 4).</summary>
internal static class QueryExecutor
{
    public static (string? LockPreambleSql, string Sql, Dictionary<string, object?> Parameters) BuildSelectSql(
        string baseSql,
        QueryBuilderStateSnapshot state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config)
    {
        var sql = baseSql;
        foreach (var include in state.Includes)
        {
            if (config.IncludeJoinSql.TryGetValue(include, out var join))
                sql += join;
            else
                throw new InvalidOperationException($"Navigation '{include}' is not available for Query().Include on this entity.");
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var whereSql = BuildWhereClause(state, resolveColumn, config, parameters);
        if (!string.IsNullOrEmpty(whereSql))
            sql += " WHERE " + whereSql;

        if (config.ApplyGlobalFilters is not null)
            sql = config.ApplyGlobalFilters(sql);

        if (state.OrderBy.Count > 0)
            sql += OrderByTranslator.Translate(state.OrderBy, resolveColumn);

        var lockSuffix = QueryLockSuffix.Get(state.LockMode, config.Provider, state.LockTimeoutMs);
        if (config.Provider == "SqlServer" && !string.IsNullOrEmpty(lockSuffix))
            sql = SqlServerTableHint.Apply(sql, lockSuffix);
        else
            sql += lockSuffix;

        string? lockPreambleSql = null;
        var lockPreambleTemplate = QueryLockSuffix.GetPreamble(state.LockMode, config.Provider, state.LockTimeoutMs);
        if (!string.IsNullOrEmpty(lockPreambleTemplate))
        {
            lockPreambleSql = config.Provider switch
            {
                "SqlServer" => $"SET LOCK_TIMEOUT {state.LockTimeoutMs}",
                "PostgreSql" => $"SET lock_timeout = {state.LockTimeoutMs}",
                _ => lockPreambleTemplate.Trim().TrimEnd(';').TrimEnd(),
            };
            if (config.Provider is not "SqlServer" and not "PostgreSql")
                parameters["lockTimeoutMs"] = state.LockTimeoutMs;
        }

        if (state.Take is not null || state.Skip is not null)
        {
            parameters["offset"] = state.Skip ?? 0;
            parameters["take"] = state.Take ?? int.MaxValue;
            sql += BuildPagingSuffix(config.Provider);
        }

        return (lockPreambleSql, sql, parameters);
    }

    public static (string? LockPreambleSql, string Sql, Dictionary<string, object?> Parameters) BuildSelectSql<T>(
        string baseSql,
        QueryBuilderState<T> state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config) where T : class
        => BuildSelectSql(baseSql, QueryBuilderStateSnapshot.From(state), resolveColumn, config);

    public static (string Sql, Dictionary<string, object?> Parameters) BuildCountSql(
        string countFromClause,
        QueryBuilderStateSnapshot state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config)
    {
        var sql = "SELECT COUNT(*)" + countFromClause;
        foreach (var include in state.Includes)
        {
            if (config.IncludeJoinSql.TryGetValue(include, out var join))
                sql += join;
        }

        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var whereSql = BuildWhereClause(state, resolveColumn, config, parameters);
        if (!string.IsNullOrEmpty(whereSql))
            sql += " WHERE " + whereSql;

        if (config.ApplyGlobalFilters is not null)
            sql = config.ApplyGlobalFilters(sql);

        return (sql, parameters);
    }

    public static (string Sql, Dictionary<string, object?> Parameters) BuildCountSql<T>(
        string countFromClause,
        QueryBuilderState<T> state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config) where T : class
        => BuildCountSql(countFromClause, QueryBuilderStateSnapshot.From(state), resolveColumn, config);

    private static string BuildWhereClause(
        QueryBuilderStateSnapshot state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config,
        Dictionary<string, object?> parameters)
    {
        var parts = new List<string>();

        if (config.SoftDeleteColumn is not null && !state.IncludeDeleted)
            parts.Add($"{config.MainAlias}.{config.SoftDeleteColumn} = 0");

        if (config.TenantIdColumn is not null)
        {
            parts.Add($"{config.MainAlias}.{config.TenantIdColumn} = @tenantId");
            if (config.GetTenantId is not null)
                parameters["tenantId"] = config.GetTenantId();
        }

        if (state.Predicates.Count > 0)
        {
            var translator = new WhereTranslator(resolveColumn, config.Provider);
            var (userSql, userParams) = translator.Translate(state.Predicates);
            if (!string.IsNullOrEmpty(userSql))
            {
                parts.Add(userSql);
                foreach (var kv in userParams)
                    parameters[kv.Key] = kv.Value;
            }
        }

        return parts.Count == 0 ? string.Empty : string.Join(" AND ", parts);
    }

    private static string BuildWhereClause<T>(
        QueryBuilderState<T> state,
        Func<string, string> resolveColumn,
        QueryRuntimeConfig config,
        Dictionary<string, object?> parameters) where T : class
        => BuildWhereClause(QueryBuilderStateSnapshot.From(state), resolveColumn, config, parameters);

    private static string BuildPagingSuffix(string provider)
        => provider is "PostgreSql" or "MySql" or "Sqlite"
            ? " LIMIT @take OFFSET @offset"
            : " OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY";
}
