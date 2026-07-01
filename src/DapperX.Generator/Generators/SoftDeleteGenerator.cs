namespace DapperX.Generator.Generators;

using DapperX.Generator.Builders;
using DapperX.Generator.Models;

/// <summary>
/// Shared soft-delete UPDATE SQL (DELETE→UPDATE rewrite). SELECT filters remain in <see cref="SqlBuilder"/>.
/// Derived queries use paired IncludeDeleted literals in <see cref="DerivedQueryGenerator"/>.
/// </summary>
internal static class SoftDeleteGenerator
{
    public static string BuildSoftDeleteUpdate(EntityModel entity, string provider, string whereClause)
    {
        var setClause = BuildSoftDeleteSetClause(entity, provider);
        return $"UPDATE {FullTable(entity)} SET {setClause} {whereClause}";
    }

    public static string BuildSoftDeleteSetClause(EntityModel entity, string provider, string? columnPrefix = null)
    {
        var prefix = columnPrefix ?? string.Empty;
        var deletedAtClause = entity.DeletedAtColumn is not null
            ? $", {prefix}{entity.DeletedAtColumn} = {AuditingSqlBuilder.CurrentTimestampLiteral(provider)}"
            : string.Empty;
        var deletedValue = provider == "PostgreSql" ? "true" : "1";
        return $"{prefix}{entity.SoftDeleteColumn} = {deletedValue}{deletedAtClause}";
    }

    private static string FullTable(EntityModel entity)
        => entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
}
