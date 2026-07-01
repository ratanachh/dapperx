namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;

internal static class UpsertGenerator
{
    public static void EmitMethodOverrides(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        if (entity.IsImmutable)
            return;

        if (entity.HasCompositeKey)
        {
            EmitUnsupportedOverrides(sb, entity, entityFqn, compositeKey: true);
            return;
        }

        EmitUpsertAsync(sb, entity, entityFqn);
        EmitUpsertManyAsync(sb, entity, entityFqn);
    }

    private static void EmitUpsertAsync(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task UpsertAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpsertAsync\";");
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        var upsertParams = entity.TenantIdColumn is not null
            ? "WithTenantParams(entity)"
            : EntityQueryEmitter.GetExecuteParameterExpression(entity);
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, UpsertSql, {upsertParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        if (entity.SecondaryTables.Any())
        {
            foreach (var st in entity.SecondaryTables)
                sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryUpdate_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUpsertManyAsync(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task UpsertManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpsertManyAsync\";");
        sb.AppendLine("        var list = entities as ICollection<" + entityFqn + "> ?? entities.ToList();");
        sb.AppendLine("        var effectiveBatchSize = batchSize ?? _options?.BatchSize ?? 1000;");
        sb.AppendLine("        foreach (var chunk in DapperX.Batching.Batch.BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        var upsertParamsMany = entity.TenantIdColumn is not null
            ? "WithTenantParams(entity)"
            : EntityQueryEmitter.GetExecuteParameterExpression(entity);
        sb.AppendLine($"                await DbExecutor.ExecuteAsync(_connection, UpsertSql, {upsertParamsMany}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        if (entity.SecondaryTables.Any())
        {
            foreach (var st in entity.SecondaryTables)
                sb.AppendLine($"                await DbExecutor.ExecuteAsync(_connection, SecondaryUpdate_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        }
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUnsupportedOverrides(StringBuilder sb, EntityModel entity, string entityFqn, bool compositeKey)
    {
        var msg = compositeKey
            ? "Upsert is not supported for composite-key entities."
            : "Upsert is not supported.";

        sb.AppendLine($"    public override Task UpsertAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine($"        => throw new NotSupportedException(\"{msg}\");");
        sb.AppendLine();
        sb.AppendLine($"    public override Task UpsertManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine($"        => throw new NotSupportedException(\"{msg}\");");
        sb.AppendLine();
    }
}
