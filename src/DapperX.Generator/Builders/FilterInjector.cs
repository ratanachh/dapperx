using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using Generator.Models;
using Generator.Utils;

internal static class FilterInjector
{
    public static string AppendSoftDeleteFilter(string sql, EntityModel entity, string provider, string? tableAlias = null)
    {
        if (entity.SoftDeleteColumn is null)
            return sql;
        var col = QualifyColumn(entity.SoftDeleteColumn, tableAlias);
        return $"{sql} AND {ProviderSqlHelper.SoftDeleteActivePredicate(col, provider)}";
    }

    public static string AppendTenantFilter(string sql, EntityModel entity, string? tableAlias = null)
    {
        if (entity.TenantIdColumn is null)
            return sql;
        var col = QualifyColumn(entity.TenantIdColumn, tableAlias);
        return $"{sql} AND {col} = @tenantId";
    }

    public static string AppendAllFilters(string sql, EntityModel entity, string provider, string? tableAlias = null)
        => AppendTenantFilter(AppendSoftDeleteFilter(sql, entity, provider, tableAlias), entity, tableAlias);

    public static string AppendJoinFilters(string onClause, EntityModel entity, string tableAlias, string provider)
    {
        if (entity.SoftDeleteColumn is not null)
            onClause += $" AND {ProviderSqlHelper.SoftDeleteActivePredicate(entity.SoftDeleteColumn, provider, tableAlias)}";
        if (entity.TenantIdColumn is not null)
            onClause += $" AND {tableAlias}.{entity.TenantIdColumn} = @tenantId";
        return onClause;
    }

    private static string QualifyColumn(string column, string? tableAlias)
        => string.IsNullOrEmpty(tableAlias) ? column : $"{tableAlias}.{column}";
}
