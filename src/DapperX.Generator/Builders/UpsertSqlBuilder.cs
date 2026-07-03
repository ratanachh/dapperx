using DapperX.Generator.Models;

namespace DapperX.Generator.Builders;

using System.Collections.Generic;
using System.Linq;
using Generator.Models;

/// <summary>Compile-time upsert SQL per provider. No lifecycle hooks; version columns excluded from match update.</summary>
internal static class UpsertSqlBuilder
{
    public static string Build(EntityModel entity, string provider)
    {
        if (entity.HasCompositeKey)
            return "SELECT 1 WHERE 1 = 0";

        return provider switch
        {
            "SqlServer" => BuildSqlServerMerge(entity, provider),
            "PostgreSql" => BuildPostgreSqlOnConflict(entity, provider),
            "MySql" => BuildMySqlOnDuplicateKey(entity, provider),
            "Sqlite" => BuildSqliteOnConflict(entity, provider),
            _ => BuildSqlServerMerge(entity, provider),
        };
    }

    private static string BuildPostgreSqlOnConflict(EntityModel entity, string provider)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var table = FullTable(entity);
        var (insertCols, insertParms) = BuildInsertLists(entity, provider);
        var updateSets = BuildConflictUpdateSets(entity, provider);

        return $"INSERT INTO {table} ({insertCols}) VALUES ({insertParms}) " +
               $"ON CONFLICT ({Col(idProp)}) DO UPDATE SET {updateSets}";
    }

    /// <summary>
    /// SQLite upsert uses <c>ON CONFLICT DO UPDATE</c> (SQLite 3.24+). Requirements also allow
    /// <c>INSERT OR REPLACE</c> for older versions; DapperX emits ON CONFLICT only (same semantics as PostgreSQL path).
    /// </summary>
    private static string BuildSqliteOnConflict(EntityModel entity, string provider)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var table = FullTable(entity);
        var (insertCols, insertParms) = BuildInsertLists(entity, provider);
        var updateSets = BuildConflictUpdateSets(entity, provider);

        return $"INSERT INTO {table} ({insertCols}) VALUES ({insertParms}) " +
               $"ON CONFLICT ({Col(idProp)}) DO UPDATE SET {updateSets}";
    }

    private static string BuildMySqlOnDuplicateKey(EntityModel entity, string provider)
    {
        var table = FullTable(entity);
        var insertable = GetInsertableProperties(entity);
        var (insertCols, insertParms) = BuildInsertLists(entity, provider);
        var updateSets = string.Join(", ",
            insertable
                .Where(p => !p.IsId && p.Updatable && !p.IsVersion)
                .Select(p => $"{Col(p)} = VALUES({Col(p)})"));

        return $"INSERT INTO {table} ({insertCols}) VALUES ({insertParms}) ON DUPLICATE KEY UPDATE {updateSets}";
    }

    private static string BuildSqlServerMerge(EntityModel entity, string provider)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var table = FullTable(entity);
        var insertable = GetInsertableProperties(entity);
        var (insertCols, _) = BuildInsertLists(entity, provider);
        var sourceCols = string.Join(", ", insertable.Select(p => $"@{p.PropertyName} AS {Col(p)}"));
        var sourceColNames = string.Join(", ", insertable.Select(Col));
        var insertValues = string.Join(", ", insertable.Select(p => $"source.{Col(p)}"));
        var updateSets = string.Join(", ",
            insertable
                .Where(p => !p.IsId && p.Updatable && !p.IsVersion)
                .Select(p => $"target.{Col(p)} = source.{Col(p)}"));

        var onClause = entity.TenantIdColumn is not null
            ? $"ON target.{Col(idProp)} = source.{Col(idProp)} AND target.{entity.TenantIdColumn} = @tenantId"
            : $"ON target.{Col(idProp)} = source.{Col(idProp)}";

        return $"MERGE {table} AS target USING (SELECT {sourceCols}) AS source ({sourceColNames}) " +
               $"{onClause} " +
               $"WHEN MATCHED THEN UPDATE SET {updateSets} " +
               $"WHEN NOT MATCHED THEN INSERT ({insertCols}) VALUES ({insertValues});";
    }

    private static (string Cols, string Parms) BuildInsertLists(EntityModel entity, string provider)
    {
        var insertable = GetInsertableProperties(entity);
        var columnNames = new HashSet<string>(insertable.Select(p => p.ColumnName), StringComparer.Ordinal);
        var colsList = insertable.Select(Col).ToList();
        var parmsList = insertable.Select(p =>
            p.ColumnTransformer?.Write is string w ? w.Replace("?", $"@{p.PropertyName}") : $"@{p.PropertyName}").ToList();

        foreach (var extra in AuditingSqlBuilder.GetInsertAssignments(entity, columnNames, provider))
        {
            colsList.Add(extra.Column);
            parmsList.Add(extra.ValueExpression);
        }

        if (entity.TenantIdColumn is not null && !columnNames.Contains(entity.TenantIdColumn))
        {
            colsList.Add(entity.TenantIdColumn);
            parmsList.Add("@tenantId");
        }

        return (string.Join(", ", colsList), string.Join(", ", parmsList));
    }

    private static string BuildConflictUpdateSets(EntityModel entity, string provider)
    {
        var updatable = entity.Properties
            .Where(p => !p.IsTransient && p.Updatable && !p.IsId && !p.IsVersion
                        && p.Formula is null && p.SecondaryTable is null)
            .ToList();

        var setColumnNames = new HashSet<string>(updatable.Select(p => p.ColumnName), StringComparer.Ordinal);
        var parts = updatable.Select(p => $"{Col(p)} = excluded.{Col(p)}").ToList();

        foreach (var extra in AuditingSqlBuilder.GetUpdateAssignments(entity, setColumnNames, provider))
            parts.Add($"{extra.Column} = excluded.{extra.Column}");

        return string.Join(", ", parts);
    }

    private static List<PropertyModel> GetInsertableProperties(EntityModel entity)
        => entity.Properties
            .Where(p => !p.IsTransient && p.Insertable && p.Formula is null && p.ColumnTransformer?.Write is null
                        && p.GeneratedTime is null && p.SecondaryTable is null)
            .ToList();

    private static string FullTable(EntityModel entity)
        => entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;

    private static string Col(PropertyModel p) => p.ColumnName;
}
