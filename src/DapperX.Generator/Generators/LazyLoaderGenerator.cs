using DapperX.Generator.Builders;
using DapperX.Generator.Models;

namespace DapperX.Generator.Generators;

using System.Linq;
using System.Text;
using Generator.Builders;
using Generator.Models;

/// <summary>Wires LazyCollection/LazyMap/LazyReference loaders on entity hydration.</summary>
internal static class LazyLoaderGenerator
{
    public static bool HasLazyRelationships(EntityModel entity)
        => entity.Relationships.Any(r =>
            (r.Kind == "OneToMany" && r.IsBatchLoadable && (r.IsLazyCollection || r.IsLazyMap))
            || (r.Kind == "ManyToMany" && r.IsLazyCollection && r.IsBatchLoadable)
            || (r.Kind == "ManyToOne" && r.IsLazyReference && !string.IsNullOrEmpty(r.TargetEntity)));

    public static bool NeedsPostLoadWiring(EntityModel entity)
        => HasLazyRelationships(entity) || entity.ElementCollections.Count > 0;

    public static void Emit(StringBuilder sb, EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels, string provider)
    {
        var lazyRels = entity.Relationships
            .Where(r =>
                (r.Kind == "OneToMany" && r.IsBatchLoadable && (r.IsLazyCollection || r.IsLazyMap))
                || (r.Kind == "ManyToMany" && r.IsLazyCollection && r.IsBatchLoadable)
                || (r.Kind == "ManyToOne" && r.IsLazyReference && !string.IsNullOrEmpty(r.TargetEntity)))
            .ToList();

        if (lazyRels.Count == 0 && entity.ElementCollections.Count == 0)
            return;

        foreach (var rel in lazyRels)
        {
            if (!TryResolveTarget(rel, allModels, out var targetModel))
                continue;

            var sql = rel.Kind switch
            {
                "ManyToMany" => RelationshipSqlBuilder.BuildManyToManySingleLoadSql(rel, targetModel),
                "ManyToOne" => RelationshipSqlBuilder.BuildManyToOneLoadSql(rel, targetModel, provider),
                _ => RelationshipSqlBuilder.BuildSingleLoadSql(rel, targetModel, provider),
            };
            sb.AppendLine($"    private const string {RelationshipSqlBuilder.GetSingleLoadSqlConstantName(rel)} = \"{Escape(sql)}\";");
        }

        sb.AppendLine();
        EmitWireLazyLoaders(sb, entity, lazyRels, allModels);
    }

    private static void EmitWireLazyLoaders(
        StringBuilder sb,
        EntityModel entity,
        IReadOnlyList<RelationshipModel> lazyRels,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var entityFqn = entity.FullyQualifiedName;
        var idProp = entity.Properties.First(p => p.IsId);

        sb.AppendLine($"    private void WireLazyLoaders({entityFqn} entity)");
        sb.AppendLine("    {");
        foreach (var rel in lazyRels)
        {
            if (!TryResolveTarget(rel, allModels, out var targetModel))
                continue;

            var sqlName = RelationshipSqlBuilder.GetSingleLoadSqlConstantName(rel);
            var targetFqn = targetModel.FullyQualifiedName;

            if (rel.Kind == "ManyToOne" && rel.IsLazyReference)
            {
                var fkProp = entity.Properties.FirstOrDefault(p =>
                    string.Equals(p.ColumnName, rel.ForeignKeyColumn, StringComparison.OrdinalIgnoreCase))
                    ?? entity.Properties.FirstOrDefault(p => p.PropertyName == rel.PropertyName + "Id");
                var fkName = fkProp?.PropertyName ?? rel.PropertyName + "Id";
                sb.AppendLine($"        entity.{rel.PropertyName} = new DapperX.Relations.Lazy.LazyReference<{targetFqn}>((conn, tx) =>");
                if (targetModel.TenantIdColumn is not null)
                    sb.AppendLine($"            DbExecutor.QueryFirstOrDefaultAsync<{targetFqn}>(conn, {sqlName}, new {{ {rel.PropertyName}Id = entity.{fkName}, tenantId = _tenantProvider?.GetCurrentTenantId() }}, tx));");
                else
                    sb.AppendLine($"            DbExecutor.QueryFirstOrDefaultAsync<{targetFqn}>(conn, {sqlName}, new {{ {rel.PropertyName}Id = entity.{fkName} }}, tx));");
                sb.AppendLine("        );");
                continue;
            }

            if (rel.IsLazyMap)
            {
                var mapKeyProp = rel.MapKeyPropertyName ?? rel.MapKeyColumn ?? "Key";
                var mapKeyType = rel.MapKeyClrTypeName ?? "object";
                sb.AppendLine($"        entity.{rel.PropertyName} = new DapperX.Relations.Lazy.LazyMap<{mapKeyType}, {targetFqn}>(r => r.{mapKeyProp}!, async (conn, tx) =>");
                sb.AppendLine("        {");
                EmitQueryRowsAssignment(sb, targetModel, targetFqn, sqlName, idProp.PropertyName);
                sb.AppendLine("            return rows;");
                sb.AppendLine("        });");
                continue;
            }

            sb.AppendLine($"        entity.{rel.PropertyName} = new DapperX.Relations.Lazy.LazyCollection<{targetFqn}>(async (conn, tx) =>");
            sb.AppendLine("        {");
            EmitQueryRowsAssignment(sb, targetModel, targetFqn, sqlName, idProp.PropertyName);
            sb.AppendLine("            return rows;");
            sb.AppendLine("        });");
        }

        foreach (var ec in entity.ElementCollections)
        {
            sb.AppendLine($"        entity.{ec.PropertyName} = new DapperX.Relations.Lazy.LazyCollection<{ec.ElementTypeName}>(async (conn, tx) =>");
            sb.AppendLine($"            (await DbExecutor.QueryAsync<{ec.ElementTypeName}>(conn, Load{ec.PropertyName}Sql, new {{ parentId = entity.{idProp.PropertyName} }}, tx)).AsList());");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        EmitOnPostLoadOverride(sb, entity, entity.HasPostLoad);
    }

    private static void EmitOnPostLoadOverride(StringBuilder sb, EntityModel entity, bool invokeLifecycle)
    {
        var entityFqn = entity.FullyQualifiedName;
        sb.AppendLine($"    protected override void OnPostLoad({entityFqn} entity)");
        sb.AppendLine("    {");
        if (invokeLifecycle)
            sb.AppendLine("        _lifecycle.InvokePostLoad(entity);");
        sb.AppendLine("        WireLazyLoaders(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitQueryRowsAssignment(
        StringBuilder sb,
        EntityModel targetModel,
        string targetFqn,
        string sqlName,
        string parentIdProperty)
    {
        if (targetModel.TenantIdColumn is not null)
            sb.AppendLine($"            var rows = (await DbExecutor.QueryAsync<{targetFqn}>(conn, {sqlName}, new {{ parentId = entity.{parentIdProperty}, tenantId = _tenantProvider?.GetCurrentTenantId() }}, tx)).AsList();");
        else
            sb.AppendLine($"            var rows = (await DbExecutor.QueryAsync<{targetFqn}>(conn, {sqlName}, new {{ parentId = entity.{parentIdProperty} }}, tx)).AsList();");
    }

    private static bool TryResolveTarget(
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel targetModel)
    {
        targetModel = null!;
        var fqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(fqn))
            return false;

        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;
        if (allModels.TryGetValue(key, out targetModel!))
            return true;

        foreach (var m in allModels.Values)
        {
            if (m.FullyQualifiedName == key || m.FullyQualifiedName == fqn)
            {
                targetModel = m;
                return true;
            }
        }

        return false;
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
