using DapperX.Generator.Emitters;
using DapperX.Generator.Generators;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using System.Collections.Generic;
using System.Linq;
using Generator.Emitters;
using Generator.Generators;
using Generator.Models;
using Generator.Utils;

/// <summary>
/// Generates SQL string literals at compile time.
/// All methods return SQL strings — never executed, only emitted as string literals in generated code.
/// </summary>
internal static class SqlBuilder
{
    private const string PrimaryAlias = "e";

    // ─── SELECT ──────────────────────────────────────────────────────────────

    /// <summary>SELECT without WHERE — used by derived-query SQL composition.</summary>
    public static string BuildSelectCore(EntityModel entity)
    {
        if (!entity.SecondaryTables.Any())
        {
            var cols = SelectColumns(entity);
            return $"SELECT {cols} FROM {FullTable(entity)}";
        }

        var qualifiedCols = SelectColumns(entity, PrimaryAlias);
        var joins = SecondaryTableJoins(entity, PrimaryAlias);
        return $"SELECT {qualifiedCols} FROM {FullTable(entity)} {PrimaryAlias}{joins}";
    }

    /// <summary>Base SELECT for runtime <c>Query()</c> — main alias + no soft-delete/tenant WHERE (composed at runtime).</summary>
    public static string BuildQueryBaseSelect(EntityModel entity, string mainAlias = "e")
    {
        var cols = entity.Properties
            .Where(p => !p.IsTransient)
            .Select(p => FormulaEmitter.FormatSelectColumn(p, mainAlias));
        var table = FullTable(entity);
        var joins = SecondaryTableJoins(entity, mainAlias);
        return $"SELECT {string.Join(", ", cols)} FROM {table} {mainAlias}{joins}";
    }

    public static string BuildQueryCountFrom(EntityModel entity, string mainAlias = "e")
    {
        var table = FullTable(entity);
        var joins = SecondaryTableJoins(entity, mainAlias);
        return $" FROM {table} {mainAlias}{joins}";
    }

    public static string BuildSelect(EntityModel entity, string? softDeleteFilter = null, string? tenantFilter = null, bool applySoftDelete = true, string provider = "SqlServer")
    {
        var sql = BuildSelectCore(entity);
        sql = AppendFilters(sql, entity, softDeleteFilter, tenantFilter, applySoftDelete, provider);
        return sql;
    }

    public static string BuildSelectById(EntityModel entity, bool applySoftDelete = true, string provider = "SqlServer")
    {
        if (entity.HasCompositeKey && entity.CompositeKey is not null)
            return $"{BuildSelectCore(entity)}{WhereFilters(entity, CompositeKeySqlHelper.BuildIdWhereClause(entity), applySoftDelete, provider)}";

        var idProp = entity.Properties.First(p => p.IsId);
        return $"{BuildSelectCore(entity)}{WhereFilters(entity, Param(entity, idProp), applySoftDelete, provider)}";
    }

    public static string BuildSelectByIds(EntityModel entity, bool applySoftDelete = true, string provider = "SqlServer")
    {
        var idProp = entity.Properties.First(p => p.IsId);
        return $"{BuildSelectCore(entity)}{WhereFilters(entity, ProviderSqlHelper.InClause(QualifyPrimaryColumn(entity, idProp.ColumnName), "ids", provider), applySoftDelete, provider)}";
    }

    public static string BuildExists(EntityModel entity, bool applySoftDelete = true, string provider = "SqlServer")
    {
        if (entity.HasCompositeKey && entity.CompositeKey is not null)
            return $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {FullTable(entity)}{WhereFilters(entity, CompositeKeySqlHelper.BuildIdWhereClause(entity), applySoftDelete, provider)}) THEN 1 ELSE 0 END";

        var idProp = entity.Properties.First(p => p.IsId);
        return $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {FullTable(entity)}{WhereFilters(entity, Param(entity, idProp), applySoftDelete, provider)}) THEN 1 ELSE 0 END";
    }

    public static string BuildCount(EntityModel entity, bool applySoftDelete = true, string provider = "SqlServer")
        => $"SELECT COUNT(*) FROM {FullTable(entity)}{WhereFilters(entity, applySoftDelete: applySoftDelete, provider: provider)}";

    // ─── INSERT ──────────────────────────────────────────────────────────────

    public static string BuildInsert(EntityModel entity, string provider)
    {
        var insertable = entity.Properties
            .Where(p => !p.IsTransient && p.Insertable && p.Formula is null && p.ColumnTransformer?.Write is null
                        && p.GeneratedTime is null && p.SecondaryTable is null
                        && !(p.IsId && p.IdGenerationStrategy == "Identity"))
            .ToList();

        var columnNames = new HashSet<string>(insertable.Select(p => p.ColumnName), StringComparer.Ordinal);
        var auditExtras = AuditingSqlBuilder.GetInsertAssignments(entity, columnNames, provider);
        foreach (var extra in auditExtras)
            columnNames.Add(extra.Column);

        var colsList = insertable.Select(p => Col(p)).ToList();
        var parmsList = insertable.Select(p =>
            p.ColumnTransformer?.Write is string w ? w.Replace("?", $"@{p.PropertyName}") : $"@{p.PropertyName}").ToList();

        foreach (var extra in auditExtras)
        {
            colsList.Add(extra.Column);
            parmsList.Add(extra.ValueExpression);
        }

        if (entity.TenantIdColumn is not null && !columnNames.Contains(entity.TenantIdColumn))
        {
            colsList.Add(entity.TenantIdColumn);
            parmsList.Add("@tenantId");
        }

        var cols = string.Join(", ", colsList);
        var parms = string.Join(", ", parmsList);

        var table = FullTable(entity);
        var sql = $"INSERT INTO {table} ({cols}) VALUES ({parms})";

        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        if (GeneratedColumnSqlBuilder.HasGeneratedProperties(entity))
            return GeneratedColumnSqlBuilder.AppendInsertGeneratedReturn(sql, entity, provider);

        // Append identity return clause per provider (sequence IDs are assigned before INSERT)
        if (idProp?.IdGenerationStrategy == "Identity")
        {
            sql = provider switch
            {
                "SqlServer"  => $"{sql}; SELECT SCOPE_IDENTITY()",
                "PostgreSql" => $"{sql} RETURNING {Col(idProp)}",
                "MySql"      => $"{sql}; SELECT LAST_INSERT_ID()",
                "Sqlite"     => $"{sql}; SELECT last_insert_rowid()",
                _            => sql,
            };
        }

        return sql;
    }

    public static string BuildSequenceNextSql(string provider, string sequenceName)
        => provider switch
        {
            "SqlServer" => $"SELECT NEXT VALUE FOR {sequenceName}",
            "PostgreSql" => $"SELECT nextval('{sequenceName}')",
            "MySql" => $"SELECT NEXTVAL({sequenceName})",
            _ => $"SELECT NEXT VALUE FOR {sequenceName}",
        };

    // ─── UPDATE ──────────────────────────────────────────────────────────────

    public static string BuildUpdate(EntityModel entity, string provider)
    {
        var versionProp = entity.Properties.FirstOrDefault(p => p.IsVersion);

        var updatable = entity.Properties
            .Where(p => !p.IsTransient && p.Updatable && !p.IsId && !p.IsVersion
                        && p.Formula is null && p.SecondaryTable is null
                        && p.GeneratedTime is null
                        && (p.ColumnTransformer is null || p.ColumnTransformer.Write is not null))
            .ToList();

        var setColumnNames = new HashSet<string>(updatable.Select(p => p.ColumnName), StringComparer.Ordinal);
        var setParts = updatable.Select(p =>
            p.ColumnTransformer?.Write is string w
                ? $"{Col(p)} = {w.Replace("?", $"@{p.PropertyName}")}"
                : $"{Col(p)} = @{p.PropertyName}").ToList();

        foreach (var extra in AuditingSqlBuilder.GetUpdateAssignments(entity, setColumnNames, provider))
            setParts.Add($"{extra.Column} = {extra.ValueExpression}");

        var sets = string.Join(", ", setParts);

        if (versionProp is not null)
            sets += $", {Col(versionProp)} = {Col(versionProp)} + 1";

        string where;
        if (entity.HasCompositeKey && entity.CompositeKey is not null)
        {
            var idWhere = CompositeKeySqlHelper.BuildEntityIdWhereClause(entity);
            where = versionProp is not null
                ? $"WHERE {idWhere} AND {QualifyPrimaryColumn(entity, versionProp.ColumnName)} = @{versionProp.PropertyName}"
                : $"WHERE {idWhere}";
        }
        else
        {
            var idProp = entity.Properties.First(p => p.IsId);
            where = versionProp is not null
                ? $"WHERE {Param(entity, idProp)} AND {QualifyPrimaryColumn(entity, versionProp.ColumnName)} = @{versionProp.PropertyName}"
                : $"WHERE {Param(entity, idProp)}";
        }

        return $"UPDATE {FullTable(entity)} SET {sets} {TenancyGenerator.AppendTenantToWhere(where, entity)}";
    }

    // ─── DELETE ──────────────────────────────────────────────────────────────

    public static string BuildDelete(EntityModel entity, string provider)
    {
        var where = BuildDeleteWhereClause(entity, includeVersion: true);

        if (entity.SoftDeleteColumn is not null)
            return SoftDeleteGenerator.BuildSoftDeleteUpdate(entity, provider, where);

        return $"DELETE FROM {FullTable(entity)} {where}";
    }

    public static string BuildDeleteById(EntityModel entity, string provider)
    {
        var where = BuildDeleteWhereClause(entity, includeVersion: false);

        if (entity.SoftDeleteColumn is not null)
            return SoftDeleteGenerator.BuildSoftDeleteUpdate(entity, provider, where);

        return $"DELETE FROM {FullTable(entity)} {where}";
    }

    public static string BuildDeleteAllByIds(EntityModel entity, string provider)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var where = TenancyGenerator.AppendTenantToWhere(
            $"WHERE {ProviderSqlHelper.InClause(QualifyPrimaryColumn(entity, idProp.ColumnName), "ids", provider)}", entity);

        if (entity.SoftDeleteColumn is not null)
            return SoftDeleteGenerator.BuildSoftDeleteUpdate(entity, provider, where);

        return $"DELETE FROM {FullTable(entity)} {where}";
    }

    private static string BuildDeleteWhereClause(EntityModel entity, bool includeVersion)
    {
        var versionProp = includeVersion ? entity.Properties.FirstOrDefault(p => p.IsVersion) : null;
        string idWhere;
        if (entity.HasCompositeKey && entity.CompositeKey is not null)
            idWhere = CompositeKeySqlHelper.BuildIdWhereClause(entity);
        else
            idWhere = Param(entity, entity.Properties.First(p => p.IsId));

        var where = versionProp is not null
            ? $"WHERE {idWhere} AND {QualifyPrimaryColumn(entity, versionProp.ColumnName)} = @{versionProp.PropertyName}"
            : $"WHERE {idWhere}";
        return TenancyGenerator.AppendTenantToWhere(where, entity);
    }

    public static string BuildHardDelete(EntityModel entity)
    {
        var where = entity.HasCompositeKey && entity.CompositeKey is not null
            ? $"WHERE {CompositeKeySqlHelper.BuildEntityIdWhereClause(entity)}"
            : $"WHERE {Param(entity, entity.Properties.First(p => p.IsId))}";
        return $"DELETE FROM {FullTable(entity)} {TenancyGenerator.AppendTenantToWhere(where, entity)}";
    }

    // ─── PAGING ──────────────────────────────────────────────────────────────

    public static string AppendPaging(string baseSql, string provider, EntityModel? entity = null)
        => provider switch
        {
            "SqlServer"  => $"{EnsureSqlServerOrderBy(baseSql, entity)} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
            _            => $"{baseSql} LIMIT @pageSize OFFSET @offset",
        };

    public static string AppendSlicePaging(string baseSql, string provider, EntityModel? entity = null)
        => provider switch
        {
            "SqlServer"  => $"{EnsureSqlServerOrderBy(baseSql, entity)} OFFSET @offset ROWS FETCH NEXT @sliceSize ROWS ONLY",
            _            => $"{baseSql} LIMIT @sliceSize OFFSET @offset",
        };

    private static string EnsureSqlServerOrderBy(string sql, EntityModel? entity)
    {
        if (sql.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase))
            return sql;

        var orderColumn = entity?.Properties.FirstOrDefault(p => p.IsId)?.ColumnName ?? "1";
        return $"{sql} ORDER BY {orderColumn}";
    }

    public static string BuildCountForPage(EntityModel entity, bool applySoftDelete = true, string provider = "SqlServer")
        => $"SELECT COUNT(*) FROM {FullTable(entity)}{WhereFilters(entity, applySoftDelete: applySoftDelete, provider: provider)}";

    // ─── GENERATED COLUMN RE-SELECT ──────────────────────────────────────────

    public static string BuildReSelectGenerated(EntityModel entity, IEnumerable<PropertyModel> generatedProps, string provider)
        => GeneratedColumnSqlBuilder.BuildReSelectSql(entity);

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static string SelectColumns(EntityModel entity, string? primaryAlias = null)
    {
        var cols = entity.Properties
            .Where(p => !p.IsTransient)
            .Select(p => FormulaEmitter.FormatSelectColumn(p, primaryAlias));
        return string.Join(", ", cols);
    }

    private static string SecondaryTableJoins(EntityModel entity, string primaryAlias = PrimaryAlias)
    {
        if (!entity.SecondaryTables.Any()) return string.Empty;
        var idColumn = entity.HasCompositeKey && entity.CompositeKey is not null
            ? entity.CompositeKey.Parts[0].ColumnName
            : entity.Properties.First(p => p.IsId).ColumnName;
        return string.Concat(entity.SecondaryTables.Select(st =>
            $" LEFT JOIN {st.TableName} st_{Sanitize(st.TableName)} ON st_{Sanitize(st.TableName)}.{st.PrimaryKeyJoinColumn} = {primaryAlias}.{idColumn}"));
    }

    private static string QualifyPrimaryColumn(EntityModel entity, string columnName)
        => entity.SecondaryTables.Any() ? $"{PrimaryAlias}.{columnName}" : columnName;

    private static string SoftDeleteActivePredicate(EntityModel entity, string provider)
        => ProviderSqlHelper.SoftDeleteActivePredicate(
            QualifyPrimaryColumn(entity, entity.SoftDeleteColumn!), provider);

    private static string WhereFilters(EntityModel entity, string? extra = null, bool applySoftDelete = true, string provider = "SqlServer")
    {
        var filters = new List<string>();
        if (applySoftDelete && entity.SoftDeleteColumn is not null)
            filters.Add(SoftDeleteActivePredicate(entity, provider));
        if (entity.TenantIdColumn is not null)
            filters.Add($"{QualifyPrimaryColumn(entity, entity.TenantIdColumn)} = @tenantId");
        if (extra is not null)
            filters.Add(extra);
        return filters.Any() ? " WHERE " + string.Join(" AND ", filters) : string.Empty;
    }

    private static string AppendFilters(string sql, EntityModel entity, string? softDeleteFilter, string? tenantFilter, bool applySoftDelete = true, string provider = "SqlServer")
    {
        var filters = new List<string>();
        if (softDeleteFilter is not null) filters.Add(softDeleteFilter);
        else if (applySoftDelete && entity.SoftDeleteColumn is not null)
            filters.Add(SoftDeleteActivePredicate(entity, provider));
        if (tenantFilter is not null) filters.Add(tenantFilter);
        else if (entity.TenantIdColumn is not null)
            filters.Add($"{QualifyPrimaryColumn(entity, entity.TenantIdColumn)} = @tenantId");
        return filters.Any() ? $"{sql} WHERE {string.Join(" AND ", filters)}" : sql;
    }

    internal static string FullTable(EntityModel entity)
        => entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;

    private static string Col(PropertyModel p) => p.ColumnName;

    private static string Param(EntityModel entity, PropertyModel p)
        => $"{QualifyPrimaryColumn(entity, p.ColumnName)} = @{p.PropertyName}";
    private static string Sanitize(string s) => string.Concat(s.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
