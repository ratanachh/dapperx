namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Builders;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class GraphGenerator
{
    public static void Emit(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType,
        IReadOnlyDictionary<string, EntityModel> allModels,
        SourceProductionContext ctx)
    {
        if (entity.IsImmutable)
            return;

        var insertRels = GraphBuilder.GetGraphRelationships(entity, allModels, GraphCascadeOperation.Insert);
        var updateRels = GraphBuilder.GetGraphRelationships(entity, allModels, GraphCascadeOperation.Update);
        var deleteRels = GraphBuilder.GetGraphRelationships(entity, allModels, GraphCascadeOperation.Delete);
        var m2mInsert = GraphBuilder.GetManyToManyGraphRelationships(entity, GraphCascadeOperation.Insert);
        var m2mUpdate = GraphBuilder.GetManyToManyGraphRelationships(entity, GraphCascadeOperation.Update);
        var m2mDelete = GraphBuilder.GetManyToManyGraphRelationships(entity, GraphCascadeOperation.Delete);

        if (!HasGraphCapableRelationships(entity))
            return;

        if (GraphBuilder.HasCycles(entity, allModels))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CircularReference,
                Location.None,
                entity.ClassName));
            return;
        }

        foreach (var rel in m2mInsert.Concat(m2mUpdate).Concat(m2mDelete).GroupBy(r => r.PropertyName).Select(g => g.First()))
            EmitJoinTableSqlConstants(sb, rel);

        if (m2mInsert.Count > 0 || m2mUpdate.Count > 0 || m2mDelete.Count > 0)
            sb.AppendLine();

        EmitInsertGraphAsync(sb, entity, entityFqn, insertRels, m2mInsert);
        EmitUpdateGraphAsync(sb, entity, entityFqn, updateRels, m2mUpdate);
        EmitDeleteGraphAsync(sb, entity, entityFqn, idType, deleteRels, m2mDelete);
    }

    private static void EmitJoinTableSqlConstants(StringBuilder sb, RelationshipModel rel)
    {
        var fk = rel.JoinTableFk ?? SyntaxHelper.ToSnakeCase(rel.PropertyName) + "_id";
        var inverseFk = rel.JoinTableInverseFk ?? "related_id";
        sb.AppendLine($"    private const string JoinInsert_{rel.PropertyName}_Sql = \"INSERT INTO {rel.JoinTable} ({fk}, {inverseFk}) VALUES (@parentId, @childId)\";");
        sb.AppendLine($"    private const string JoinDelete_{rel.PropertyName}_Sql = \"DELETE FROM {rel.JoinTable} WHERE {fk} = @parentId\";");
    }

    private static void EmitInsertGraphAsync(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        IReadOnlyList<GraphRelationshipInfo> graphRels,
        IReadOnlyList<RelationshipModel> manyToManyRels)
    {
        sb.AppendLine($"    public override async Task InsertGraphAsync({entityFqn} root, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"InsertGraphAsync\";");
        sb.AppendLine("        DapperX.Runtime.Logging.SqlExecutionLogger.TryLogBatchTrace(DbExecutor.CreateLogContext(MethodName, Options, Provider), InsertSql, 1, 1);");
        sb.AppendLine("        var ownsTransaction = transaction is null;");
        sb.AppendLine("        transaction ??= _connection.BeginTransaction();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePrePersistBatch(new[] { root });");
        sb.AppendLine("            OnPrePersist(root);");
        sb.AppendLine("            await InsertAsync(root, transaction, ct);");
        foreach (var rel in graphRels)
            EmitInsertChildCollection(sb, entity, rel);

        EmitManyToManyInsertLoop(sb, entity, manyToManyRels);
        sb.AppendLine("            OnPostPersist(root);");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePostPersistBatch(new[] { root });");
        sb.AppendLine("            if (ownsTransaction) transaction.Commit();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            if (ownsTransaction) transaction.Rollback();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitInsertChildCollection(StringBuilder sb, EntityModel entity, GraphRelationshipInfo rel)
    {
        sb.AppendLine($"            var {rel.Relationship.PropertyName}Children = root.{rel.Relationship.PropertyName}.TryGet();");
        sb.AppendLine($"            if ({rel.Relationship.PropertyName}Children is not null && {rel.Relationship.PropertyName}Children.Count > 0)");
        sb.AppendLine("            {");
        sb.AppendLine($"                foreach (var child in {rel.Relationship.PropertyName}Children)");
        sb.AppendLine($"                    child.{rel.FkPropertyName} = root.Id;");
        sb.AppendLine($"                var {rel.Relationship.PropertyName}Repo = {EmitChildRepository(entity, rel.ChildEntity)};");
        sb.AppendLine($"                await {rel.Relationship.PropertyName}Repo.InsertManyAsync({rel.Relationship.PropertyName}Children, transaction, ct);");
        if (!string.IsNullOrEmpty(rel.Relationship.OrderColumnName))
        {
            var childIdProp = rel.ChildEntity.Properties.First(p => p.IsId).PropertyName;
            var parentIdProp = entity.Properties.First(p => p.IsId).PropertyName;
            sb.AppendLine("                var __position = 0;");
            sb.AppendLine($"                foreach (var child in {rel.Relationship.PropertyName}Children)");
            sb.AppendLine("                {");
            sb.AppendLine("                    __position++;");
            sb.AppendLine($"                    await DbExecutor.ExecuteAsync(_connection, Assign{rel.Relationship.PropertyName}OrderPositionSql, new {{ parentId = root.{parentIdProp}, childId = child.{childIdProp}, position = __position }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("                }");
        }

        sb.AppendLine("            }");
    }

    private static void EmitManyToManyInsertLoop(StringBuilder sb, EntityModel entity, IReadOnlyList<RelationshipModel> manyToManyRels)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        foreach (var rel in manyToManyRels)
        {
            var childIdProp = ResolveManyToManyChildIdProperty(rel);
            sb.AppendLine($"            var {rel.PropertyName}Links = root.{rel.PropertyName}.TryGet();");
            sb.AppendLine($"            if ({rel.PropertyName}Links is not null && {rel.PropertyName}Links.Count > 0)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var joinRows = {rel.PropertyName}Links");
            sb.AppendLine($"                    .Select(child => new {{ parentId = root.{idProp.PropertyName}, childId = (object?)child.{childIdProp} }})");
            sb.AppendLine("                    .Where(x => x.childId is not null)");
            sb.AppendLine("                    .Distinct()");
            sb.AppendLine("                    .ToList();");
            sb.AppendLine("                if (joinRows.Count > 0)");
            sb.AppendLine($"                    await DbExecutor.ExecuteAsync(_connection, JoinInsert_{rel.PropertyName}_Sql, joinRows, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("            }");
        }
    }

    private static string ResolveManyToManyChildIdProperty(RelationshipModel rel) => "Id";

    private static void EmitUpdateGraphAsync(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        IReadOnlyList<GraphRelationshipInfo> graphRels,
        IReadOnlyList<RelationshipModel> manyToManyRels)
    {
        sb.AppendLine($"    public override async Task UpdateGraphAsync({entityFqn} root, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpdateGraphAsync\";");
        sb.AppendLine("        DapperX.Runtime.Logging.SqlExecutionLogger.TryLogBatchTrace(DbExecutor.CreateLogContext(MethodName, Options, Provider), UpdateSql, 1, 1);");
        sb.AppendLine("        var ownsTransaction = transaction is null;");
        sb.AppendLine("        transaction ??= _connection.BeginTransaction();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePreUpdateBatch(new[] { root });");
        sb.AppendLine("            OnPreUpdate(root);");
        sb.AppendLine("            await UpdateAsync(root, transaction, ct);");
        foreach (var rel in graphRels)
        {
            sb.AppendLine($"            var {rel.Relationship.PropertyName}Children = root.{rel.Relationship.PropertyName}.TryGet();");
            sb.AppendLine($"            if ({rel.Relationship.PropertyName}Children is not null && {rel.Relationship.PropertyName}Children.Count > 0)");
            sb.AppendLine("            {");
            sb.AppendLine($"                var {rel.Relationship.PropertyName}Repo = {EmitChildRepository(entity, rel.ChildEntity)};");
            sb.AppendLine($"                await {rel.Relationship.PropertyName}Repo.UpdateManyAsync({rel.Relationship.PropertyName}Children, transaction, ct);");
            sb.AppendLine("            }");
        }

        EmitManyToManyUpdateLoop(sb, entity, manyToManyRels);
        sb.AppendLine("            OnPostUpdate(root);");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePostUpdateBatch(new[] { root });");
        sb.AppendLine("            if (ownsTransaction) transaction.Commit();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            if (ownsTransaction) transaction.Rollback();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitManyToManyUpdateLoop(StringBuilder sb, EntityModel entity, IReadOnlyList<RelationshipModel> manyToManyRels)
    {
        if (manyToManyRels.Count == 0)
            return;

        var idProp = entity.Properties.First(p => p.IsId);
        foreach (var rel in manyToManyRels)
        {
            var childIdProp = ResolveManyToManyChildIdProperty(rel);
            sb.AppendLine($"            var {rel.PropertyName}Links = root.{rel.PropertyName}.TryGet();");
            sb.AppendLine($"            if ({rel.PropertyName}Links is not null)");
            sb.AppendLine("            {");
            sb.AppendLine($"                await DbExecutor.ExecuteAsync(_connection, JoinDelete_{rel.PropertyName}_Sql, new {{ parentId = root.{idProp.PropertyName} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine($"                if ({rel.PropertyName}Links.Count > 0)");
            sb.AppendLine("                {");
            sb.AppendLine($"                    var joinRows = {rel.PropertyName}Links");
            sb.AppendLine($"                        .Select(child => new {{ parentId = root.{idProp.PropertyName}, childId = (object?)child.{childIdProp} }})");
            sb.AppendLine("                        .Where(x => x.childId is not null)");
            sb.AppendLine("                        .Distinct()");
            sb.AppendLine("                        .ToList();");
            sb.AppendLine("                    if (joinRows.Count > 0)");
            sb.AppendLine($"                        await DbExecutor.ExecuteAsync(_connection, JoinInsert_{rel.PropertyName}_Sql, joinRows, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
        }
    }

    private static void EmitDeleteGraphAsync(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string idType,
        IReadOnlyList<GraphRelationshipInfo> graphRels,
        IReadOnlyList<RelationshipModel> manyToManyRels)
    {
        sb.AppendLine($"    public override async Task DeleteGraphAsync({entityFqn} root, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteGraphAsync\";");
        sb.AppendLine("        DapperX.Runtime.Logging.SqlExecutionLogger.TryLogBatchTrace(DbExecutor.CreateLogContext(MethodName, Options, Provider), DeleteSql, 1, 1);");
        sb.AppendLine("        var ownsTransaction = transaction is null;");
        sb.AppendLine("        transaction ??= _connection.BeginTransaction();");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        var idProp = entity.Properties.First(p => p.IsId);
        foreach (var rel in graphRels.AsEnumerable().Reverse())
        {
            sb.AppendLine($"            var {rel.Relationship.PropertyName}Children = root.{rel.Relationship.PropertyName}.TryGet();");
            sb.AppendLine($"            if ({rel.Relationship.PropertyName}Children is not null && {rel.Relationship.PropertyName}Children.Count > 0)");
            sb.AppendLine("            {");
            if (!string.IsNullOrEmpty(rel.Relationship.OrderColumnName))
            {
                var orderProp = rel.ChildEntity.Properties
                    .FirstOrDefault(p => string.Equals(p.ColumnName, rel.Relationship.OrderColumnName, StringComparison.OrdinalIgnoreCase))
                    ?.PropertyName ?? rel.Relationship.OrderColumnName;
                sb.AppendLine($"                foreach (var child in {rel.Relationship.PropertyName}Children.OrderByDescending(c => c.{orderProp}))");
                sb.AppendLine("                {");
                sb.AppendLine($"                    await Close{rel.Relationship.PropertyName}OrderGapAsync(root.{idProp.PropertyName}, child.{orderProp}, transaction);");
                sb.AppendLine("                }");
            }

            sb.AppendLine($"                var {rel.Relationship.PropertyName}Repo = {EmitChildRepository(entity, rel.ChildEntity)};");
            sb.AppendLine($"                await {rel.Relationship.PropertyName}Repo.DeleteManyAsync({rel.Relationship.PropertyName}Children, transaction, ct);");
            sb.AppendLine("            }");
        }

        foreach (var rel in manyToManyRels)
            sb.AppendLine($"            await DbExecutor.ExecuteAsync(_connection, JoinDelete_{rel.PropertyName}_Sql, new {{ parentId = root.{idProp.PropertyName} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");

        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePreRemoveBatch(new[] { root });");
        sb.AppendLine("            OnPreRemove(root);");
        sb.AppendLine("            await DeleteAsync(root, transaction, ct);");
        sb.AppendLine("            OnPostRemove(root);");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("            _batchLifecycle.InvokePostRemoveBatch(new[] { root });");
        sb.AppendLine("            if (ownsTransaction) transaction.Commit();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch");
        sb.AppendLine("        {");
        sb.AppendLine("            if (ownsTransaction) transaction.Rollback();");
        sb.AppendLine("            throw;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string EmitChildRepository(EntityModel parentEntity, EntityModel childEntity)
        => GraphChildRepositoryEmitter.EmitNewExpression(parentEntity, childEntity);

    private static bool HasGraphCapableRelationships(EntityModel entity)
        => entity.Relationships.Any(r =>
            (r.Kind == "OneToMany" && r.IsLazyCollection)
            || (r.Kind == "ManyToMany" && r.IsLazyCollection && !string.IsNullOrEmpty(r.JoinTable)));
}
