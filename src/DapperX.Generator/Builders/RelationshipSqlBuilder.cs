using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using System.Collections.Generic;
using System.Linq;
using Generator.Models;
using Generator.Utils;

/// <summary>Compile-time SELECT literals for batch relationship loading.</summary>
internal static class RelationshipSqlBuilder
{
    public static string BuildBatchLoadSql(RelationshipModel rel, EntityModel childEntity, string provider)
    {
        var select = SqlBuilder.BuildSelectCore(childEntity);
        var idColumn = childEntity.Properties.First(p => p.IsId).ColumnName;
        var filters = new List<string>();

        if (rel.IsPrimaryKeyJoin)
            filters.Add(ProviderSqlHelper.InClause(idColumn, "parentIds", provider));
        else
            filters.Add(ProviderSqlHelper.InClause(rel.ForeignKeyColumn, "parentIds", provider));

        if (childEntity.SoftDeleteColumn is not null)
            filters.Add(ProviderSqlHelper.SoftDeleteActivePredicate(childEntity.SoftDeleteColumn, provider));

        if (childEntity.TenantIdColumn is not null)
            filters.Add($"{childEntity.TenantIdColumn} = @tenantId");

        var sql = $"{select} WHERE {string.Join(" AND ", filters)}";

        if (!string.IsNullOrEmpty(rel.OrderByClause))
        {
            sql += $" ORDER BY {rel.OrderByClause}";
        }
        else if (!string.IsNullOrEmpty(rel.OrderColumnName))
        {
            sql += $" ORDER BY {rel.OrderColumnName}";
        }

        return sql;
    }

    public static string BuildReferenceJoinSql(
        EntityModel mainEntity,
        string mainAlias,
        RelationshipModel rel,
        EntityModel targetEntity)
    {
        var joinAlias = $"nav_{rel.PropertyName}";
        var targetTable = targetEntity.Schema is not null
            ? $"{targetEntity.Schema}.{targetEntity.TableName}"
            : targetEntity.TableName;

        if (targetEntity.CompositeKey is { } compositeKey)
        {
            var onClause = BuildCompositeReferenceJoinOn(mainEntity, mainAlias, joinAlias, compositeKey);
            if (!string.IsNullOrEmpty(onClause))
                return $" INNER JOIN {targetTable} {joinAlias} ON {onClause}";
        }

        var targetId = targetEntity.Properties.FirstOrDefault(p => p.IsId)?.ColumnName
            ?? targetEntity.CompositeKey?.Parts[0].ColumnName
            ?? "id";

        if (rel.IsPrimaryKeyJoin)
            return $" INNER JOIN {targetTable} {joinAlias} ON {mainAlias}.{targetId} = {joinAlias}.{targetId}";

        return $" INNER JOIN {targetTable} {joinAlias} ON {mainAlias}.{rel.ForeignKeyColumn} = {joinAlias}.{targetId}";
    }

    private static string BuildCompositeReferenceJoinOn(
        EntityModel mainEntity,
        string mainAlias,
        string joinAlias,
        CompositeKeyModel compositeKey)
    {
        var onClauses = new List<string>();
        foreach (var part in compositeKey.Parts)
        {
            var fkProp = mainEntity.Properties.FirstOrDefault(p =>
                string.Equals(p.PropertyName, part.KeyClassPropertyName, StringComparison.Ordinal));
            if (fkProp is null)
                return string.Empty;

            onClauses.Add($"{mainAlias}.{fkProp.ColumnName} = {joinAlias}.{part.ColumnName}");
        }

        return string.Join(" AND ", onClauses);
    }

    public static string BuildAssignOrderPositionSql(RelationshipModel rel, EntityModel childEntity)
    {
        var idColumn = childEntity.Properties.First(p => p.IsId).ColumnName;
        return $"UPDATE {FullChildTable(childEntity)} SET {rel.OrderColumnName} = @position WHERE {idColumn} = @childId";
    }

    public static string BuildCloseOrderGapSql(RelationshipModel rel, EntityModel childEntity)
    {
        return $"UPDATE {FullChildTable(childEntity)} " +
               $"SET {rel.OrderColumnName} = {rel.OrderColumnName} - 1 " +
               $"WHERE {rel.ForeignKeyColumn} = @parentId AND {rel.OrderColumnName} > @position";
    }

    public static string GetSqlConstantName(RelationshipModel rel)
        => $"Load{rel.PropertyName}ForManySql";

    public static string GetSingleLoadSqlConstantName(RelationshipModel rel)
        => $"Load{rel.PropertyName}Sql";

    public static string BuildSingleLoadSql(RelationshipModel rel, EntityModel childEntity, string provider)
    {
        var select = SqlBuilder.BuildSelectCore(childEntity);
        var idColumn = childEntity.Properties.First(p => p.IsId).ColumnName;
        var filters = new List<string>();

        if (rel.IsPrimaryKeyJoin)
            filters.Add($"{idColumn} = @parentId");
        else
            filters.Add($"{rel.ForeignKeyColumn} = @parentId");

        if (childEntity.SoftDeleteColumn is not null)
            filters.Add(ProviderSqlHelper.SoftDeleteActivePredicate(childEntity.SoftDeleteColumn, provider));

        if (childEntity.TenantIdColumn is not null)
            filters.Add($"{childEntity.TenantIdColumn} = @tenantId");

        var sql = $"{select} WHERE {string.Join(" AND ", filters)}";

        if (!string.IsNullOrEmpty(rel.OrderByClause))
            sql += $" ORDER BY {rel.OrderByClause}";
        else if (!string.IsNullOrEmpty(rel.OrderColumnName))
            sql += $" ORDER BY {rel.OrderColumnName}";

        return sql;
    }

    public static string BuildManyToManySingleLoadSql(RelationshipModel rel, EntityModel targetEntity)
    {
        var targetTable = FullChildTable(targetEntity);
        var targetId = targetEntity.Properties.First(p => p.IsId).ColumnName;
        var fk = rel.JoinTableFk ?? "parent_id";
        var inverseFk = rel.JoinTableInverseFk ?? "related_id";
        var joinTable = rel.JoinTable!;

        return $"SELECT t.* FROM {targetTable} t INNER JOIN {joinTable} j ON j.{inverseFk} = t.{targetId} WHERE j.{fk} = @parentId";
    }

    public static string BuildManyToManyBatchLoadSql(RelationshipModel rel, EntityModel targetEntity, string provider)
    {
        var fk = rel.JoinTableFk ?? "parent_id";
        var inverseFk = rel.JoinTableInverseFk ?? "related_id";
        var joinTable = rel.JoinTable!;
        return $"SELECT {fk} AS ParentId, {inverseFk} AS ChildId FROM {joinTable} WHERE {ProviderSqlHelper.InClause(fk, "parentIds", provider)}";
    }

    public static string GetManyToManyLinksSqlConstantName(RelationshipModel rel)
        => $"Load{rel.PropertyName}LinksForManySql";

    public static string BuildManyToOneLoadSql(RelationshipModel rel, EntityModel targetEntity, string provider)
    {
        var idProp = targetEntity.Properties.First(p => p.IsId);
        var select = SqlBuilder.BuildSelectCore(targetEntity);
        var filters = new List<string> { $"{idProp.ColumnName} = @{rel.PropertyName}Id" };

        if (targetEntity.SoftDeleteColumn is not null)
            filters.Add(ProviderSqlHelper.SoftDeleteActivePredicate(targetEntity.SoftDeleteColumn, provider));

        if (targetEntity.TenantIdColumn is not null)
            filters.Add($"{targetEntity.TenantIdColumn} = @tenantId");

        return $"{select} WHERE {string.Join(" AND ", filters)}";
    }

    private static string FullChildTable(EntityModel childEntity)
        => childEntity.Schema is not null
            ? $"{childEntity.Schema}.{childEntity.TableName}"
            : childEntity.TableName;
}
