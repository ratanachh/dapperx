using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Builders;

using System.Collections.Generic;
using System.Linq;
using Generator.Models;

/// <summary>Provider-specific INSERT return / post-INSERT re-SELECT for <c>[Generated]</c> columns (Rule A).</summary>
internal static class GeneratedColumnSqlBuilder
{
    public static bool HasGeneratedProperties(EntityModel entity)
        => entity.Properties.Any(p => p.GeneratedTime is not null);

    public static IReadOnlyList<PropertyModel> GetGeneratedProperties(EntityModel entity)
        => entity.Properties.Where(p => p.GeneratedTime is not null).ToList();

    public static bool UsesInlineInsertOutput(string provider)
        => provider is "SqlServer" or "PostgreSql";

    public static bool UsesSeparateInsertReSelect(string provider)
        => !UsesInlineInsertOutput(provider);

    public static bool NeedsReSelectConstant(EntityModel entity, string provider)
    {
        if (!HasGeneratedProperties(entity))
            return false;
        if (entity.Properties.Any(p => p.GeneratedTime == "Always"))
            return true;
        return UsesSeparateInsertReSelect(provider);
    }

    /// <summary>Appends identity/generated return clause to a base INSERT (no trailing identity scalar).</summary>
    public static string AppendInsertGeneratedReturn(string sql, EntityModel entity, string provider)
    {
        var generated = GetGeneratedProperties(entity);
        if (generated.Count == 0)
            return sql;

        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        var useIdentity = idProp?.IdGenerationStrategy == "Identity";

        return provider switch
        {
            "SqlServer" => AppendSqlServerOutput(sql, generated, idProp, useIdentity),
            "PostgreSql" => AppendPostgreSqlReturning(sql, generated, idProp, useIdentity),
            _ => AppendMySqlSqliteIdentityReturn(sql, idProp, useIdentity, provider),
        };
    }

    public static string BuildReSelectSql(EntityModel entity)
    {
        var generated = GetGeneratedProperties(entity);
        if (generated.Count == 0)
            return string.Empty;

        var idProp = entity.Properties.First(p => p.IsId);
        var idCol = entity.SecondaryTables.Any() ? $"e.{idProp.ColumnName}" : idProp.ColumnName;
        var cols = string.Join(", ", generated.Select(p => p.ColumnName));
        var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
        var from = entity.SecondaryTables.Any() ? $"{table} e" : table;
        return $"SELECT {cols} FROM {from} WHERE {idCol} = @{idProp.PropertyName}";
    }

    private static string AppendSqlServerOutput(
        string sql,
        IReadOnlyList<PropertyModel> generated,
        PropertyModel? idProp,
        bool useIdentity)
    {
        var outputCols = new List<string>();
        if (useIdentity && idProp is not null)
            outputCols.Add($"INSERTED.{idProp.ColumnName}");
        foreach (var p in generated)
            outputCols.Add($"INSERTED.{p.ColumnName}");

        var outputClause = $" OUTPUT {string.Join(", ", outputCols)}";
        var valuesIdx = sql.IndexOf(" VALUES (", StringComparison.OrdinalIgnoreCase);
        if (valuesIdx < 0)
            return sql;
        return sql.Insert(valuesIdx, outputClause);
    }

    private static string AppendPostgreSqlReturning(
        string sql,
        IReadOnlyList<PropertyModel> generated,
        PropertyModel? idProp,
        bool useIdentity)
    {
        var returningCols = new List<string>();
        if (useIdentity && idProp is not null)
            returningCols.Add(idProp.ColumnName);
        foreach (var p in generated)
            returningCols.Add(p.ColumnName);

        return $"{sql} RETURNING {string.Join(", ", returningCols)}";
    }

    private static string AppendMySqlSqliteIdentityReturn(string sql, PropertyModel? idProp, bool useIdentity, string provider)
    {
        if (!useIdentity || idProp is null)
            return sql;

        return provider switch
        {
            "MySql" => $"{sql}; SELECT LAST_INSERT_ID()",
            "Sqlite" => $"{sql}; SELECT last_insert_rowid()",
            _ => sql,
        };
    }
}
