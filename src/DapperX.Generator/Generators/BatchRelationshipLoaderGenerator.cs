using DapperX.Generator.Builders;
using DapperX.Generator.Models;

namespace DapperX.Generator.Generators;

using System.Linq;
using System.Text;
using Generator.Builders;
using Generator.Models;

internal static class BatchRelationshipLoaderGenerator
{
    public static void EmitChildLifecycleFields(StringBuilder sb, EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var childModels = new List<EntityModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rel in entity.Relationships.Where(r => r.IsBatchLoadable && r.ChildHasPostLoad && !string.IsNullOrEmpty(r.ChildEntityFqn)))
        {
            if (!TryGetChildModelByFqn(rel.ChildEntityFqn!, allModels, out var m))
                continue;
            if (seen.Add(m.FullyQualifiedName))
                childModels.Add(m);
        }

        foreach (var childModel in childModels)
        {
            var fieldName = GetChildLifecycleFieldName(childModel);
            sb.AppendLine($"    private readonly {childModel.ClassName}LifecycleInvoker {fieldName} = new();");
        }

        if (childModels.Count > 0)
            sb.AppendLine();
    }

    public static void Emit(StringBuilder sb, EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels, string provider)
    {
        var batchRels = entity.Relationships
            .Where(r => (r.Kind == "OneToMany" && r.IsBatchLoadable)
                        || (r.Kind == "ManyToMany" && r.IsLazyCollection && r.IsBatchLoadable))
            .ToList();

        if (batchRels.Count == 0)
            return;

        foreach (var rel in batchRels)
        {
            if (!TryGetChildModel(rel, allModels, out var childModel))
                continue;
            EmitSqlConstant(sb, rel, childModel, provider);
            EmitOrderColumnSqlConstants(sb, rel, childModel);
        }

        sb.AppendLine();

        foreach (var rel in batchRels)
        {
            if (!TryGetChildModel(rel, allModels, out var childModel))
                continue;

            if (rel.IsLazyMap)
                EmitLazyMapLoader(sb, entity, rel, childModel, allModels);
            else if (rel.Kind == "ManyToMany")
                EmitManyToManyLoader(sb, entity, rel, childModel, provider);
            else
                EmitLazyCollectionLoader(sb, entity, rel, childModel, allModels);
        }
    }

    private static void EmitSqlConstant(StringBuilder sb, RelationshipModel rel, EntityModel childModel, string provider)
    {
        var sql = rel.Kind == "ManyToMany"
            ? RelationshipSqlBuilder.BuildManyToManyBatchLoadSql(rel, childModel, provider)
            : RelationshipSqlBuilder.BuildBatchLoadSql(rel, childModel, provider);
        var name = rel.Kind == "ManyToMany"
            ? RelationshipSqlBuilder.GetManyToManyLinksSqlConstantName(rel)
            : RelationshipSqlBuilder.GetSqlConstantName(rel);
        sb.AppendLine($"    private const string {name} = \"{EscapeString(sql)}\";");
    }

    private static void EmitManyToManyLoader(
        StringBuilder sb,
        EntityModel parent,
        RelationshipModel rel,
        EntityModel targetModel,
        string provider)
    {
        var parentFqn = parent.FullyQualifiedName;
        var targetFqn = targetModel.FullyQualifiedName;
        var idProp = parent.Properties.First(p => p.IsId);
        var targetIdProp = targetModel.Properties.First(p => p.IsId);
        var linksSqlName = RelationshipSqlBuilder.GetManyToManyLinksSqlConstantName(rel);
        var selectByIdsSql = SqlBuilder.BuildSelectByIds(targetModel, provider: provider);

        sb.AppendLine($"    private const string Load{rel.PropertyName}ChildSelectByIdsSql = \"{EscapeString(selectByIdsSql)}\";");
        sb.AppendLine();
        sb.AppendLine($"    public async Task Load{rel.PropertyName}ForManyAsync(IEnumerable<{parentFqn}> parents, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Load{rel.PropertyName}ForManyAsync\";");
        sb.AppendLine($"        var parentIds = parents.Select(p => p.{idProp.PropertyName}).ToList();");
        sb.AppendLine("        if (parentIds.Count == 0) return;");
        sb.AppendLine($"        var links = (await DbExecutor.QueryAsync<({idProp.ClrTypeName} ParentId, {targetIdProp.ClrTypeName} ChildId)>(_connection, {linksSqlName}, new {{ parentIds }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider))).ToList();");
        sb.AppendLine("        var childIds = links.Select(l => l.ChildId).Distinct().ToList();");
        sb.AppendLine("        if (childIds.Count == 0) return;");
        var childSelectSql = targetModel.GlobalFilters.Any()
            ? GlobalFilterGenerator.FilteredChildSql(targetModel, $"Load{rel.PropertyName}ChildSelectByIdsSql")
            : $"Load{rel.PropertyName}ChildSelectByIdsSql";
        sb.AppendLine($"        var children = (await DbExecutor.QueryAsync<{targetFqn}>(_connection, {childSelectSql}, new {{ ids = childIds }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider))).ToDictionary(c => c.{targetIdProp.PropertyName});");
        sb.AppendLine("        var grouped = links.ToLookup(l => l.ParentId, l => children[l.ChildId]);");
        sb.AppendLine("        foreach (var parent in parents)");
        sb.AppendLine("        {");
        sb.AppendLine($"            parent.{rel.PropertyName}.Set(grouped[parent.{idProp.PropertyName}]);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitOrderColumnSqlConstants(StringBuilder sb, RelationshipModel rel, EntityModel childModel)
    {
        if (string.IsNullOrEmpty(rel.OrderColumnName))
            return;

        var assignSql = RelationshipSqlBuilder.BuildAssignOrderPositionSql(rel, childModel);
        var gapSql = RelationshipSqlBuilder.BuildCloseOrderGapSql(rel, childModel);
        sb.AppendLine($"    private const string Assign{rel.PropertyName}OrderPositionSql = \"{EscapeString(assignSql)}\";");
        sb.AppendLine($"    private const string Close{rel.PropertyName}OrderGapSql = \"{EscapeString(gapSql)}\";");
    }

    private static void EmitLazyCollectionLoader(
        StringBuilder sb,
        EntityModel parent,
        RelationshipModel rel,
        EntityModel childModel,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var parentFqn = parent.FullyQualifiedName;
        var childFqn = childModel.FullyQualifiedName;
        var idProp = parent.Properties.First(p => p.IsId);
        var sqlName = RelationshipSqlBuilder.GetSqlConstantName(rel);

        sb.AppendLine($"    public async Task Load{rel.PropertyName}ForManyAsync(IEnumerable<{parentFqn}> parents, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Load{rel.PropertyName}ForManyAsync\";");
        sb.AppendLine($"        var parentIds = parents.Select(p => p.{idProp.PropertyName}).ToList();");
        sb.AppendLine("        if (parentIds.Count == 0) return;");
        EmitQueryAndGroup(sb, childModel, childFqn, sqlName);
        sb.AppendLine($"        var grouped = rows.ToLookup(r => r.{rel.FkPropertyNameOnChild});");
        sb.AppendLine("        foreach (var parent in parents)");
        sb.AppendLine("        {");
        sb.AppendLine($"            parent.{rel.PropertyName}.Set(grouped[parent.{idProp.PropertyName}]);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        EmitOrderColumnMethods(sb, parent, rel, childModel);
    }

    private static void EmitLazyMapLoader(
        StringBuilder sb,
        EntityModel parent,
        RelationshipModel rel,
        EntityModel childModel,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var parentFqn = parent.FullyQualifiedName;
        var childFqn = childModel.FullyQualifiedName;
        var idProp = parent.Properties.First(p => p.IsId);
        var sqlName = RelationshipSqlBuilder.GetSqlConstantName(rel);
        var mapKeyProp = rel.MapKeyPropertyName ?? rel.MapKeyColumn ?? "Key";

        sb.AppendLine($"    public async Task Load{rel.PropertyName}ForManyAsync(IEnumerable<{parentFqn}> parents, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Load{rel.PropertyName}ForManyAsync\";");
        sb.AppendLine($"        var parentIds = parents.Select(p => p.{idProp.PropertyName}).ToList();");
        sb.AppendLine("        if (parentIds.Count == 0) return;");
        EmitQueryAndGroup(sb, childModel, childFqn, sqlName);
        sb.AppendLine($"        var byParent = rows.ToLookup(r => r.{rel.FkPropertyNameOnChild});");
        sb.AppendLine("        foreach (var parent in parents)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var map = byParent[parent.{idProp.PropertyName}]");
        sb.AppendLine($"                .ToDictionary(r => r.{mapKeyProp});");
        sb.AppendLine($"            parent.{rel.PropertyName}.Set(map);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        EmitOrderColumnMethods(sb, parent, rel, childModel);
    }

    private static void EmitOrderColumnMethods(
        StringBuilder sb,
        EntityModel parent,
        RelationshipModel rel,
        EntityModel childModel)
    {
        if (string.IsNullOrEmpty(rel.OrderColumnName))
            return;

        var idProp = parent.Properties.First(p => p.IsId);
        var childIdType = childModel.Properties.First(p => p.IsId).ClrTypeName;

        sb.AppendLine($"    public async Task Assign{rel.PropertyName}PositionAsync({idProp.ClrTypeName} parentId, {childIdType} childId, int position, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Assign{rel.PropertyName}PositionAsync\";");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, Assign{rel.PropertyName}OrderPositionSql, new {{ parentId, childId, position }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine($"    public async Task Close{rel.PropertyName}OrderGapAsync({idProp.ClrTypeName} parentId, int position, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"Close{rel.PropertyName}OrderGapAsync\";");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, Close{rel.PropertyName}OrderGapSql, new {{ parentId, position }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitQueryAndGroup(StringBuilder sb, EntityModel childModel, string childFqn, string sqlName)
    {
        var sqlExpr = childModel.GlobalFilters.Any()
            ? GlobalFilterGenerator.FilteredChildSql(childModel, sqlName)
            : sqlName;
        if (childModel.TenantIdColumn is not null)
        {
            sb.AppendLine("        var tenantId = _tenantProvider?.GetCurrentTenantId();");
            sb.AppendLine($"        var rows = (await DbExecutor.QueryAsync<{childFqn}>(_connection, {sqlExpr}, new {{ parentIds, tenantId }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider))).ToList();");
        }
        else
        {
            sb.AppendLine($"        var rows = (await DbExecutor.QueryAsync<{childFqn}>(_connection, {sqlExpr}, new {{ parentIds }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider))).ToList();");
        }

        if (childModel.HasPostLoad)
        {
            var fieldName = GetChildLifecycleFieldName(childModel);
            sb.AppendLine("        foreach (var row in rows)");
            sb.AppendLine($"            {fieldName}.InvokePostLoad(row);");
        }
    }

    private static bool TryGetChildModel(
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel childModel)
        => TryGetChildModelByFqn(rel.ChildEntityFqn ?? string.Empty, allModels, out childModel);

    private static bool TryGetChildModelByFqn(
        string childFqn,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel childModel)
    {
        childModel = null!;
        var key = NormalizeFqn(childFqn);
        if (allModels.TryGetValue(key, out childModel!))
            return true;

        foreach (var model in allModels.Values)
        {
            if (model.FullyQualifiedName == childFqn || NormalizeFqn(model.FullyQualifiedName) == key)
            {
                childModel = model;
                return true;
            }
        }

        return false;
    }

    private static string GetChildLifecycleFieldName(EntityModel childModel)
    {
        var name = childModel.ClassName;
        return name.Length == 0
            ? "_childLifecycle"
            : $"_{char.ToLowerInvariant(name[0])}{name.Substring(1)}Lifecycle";
    }

    private static string NormalizeFqn(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;

    private static string EscapeString(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
