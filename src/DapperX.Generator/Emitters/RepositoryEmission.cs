using DapperX.Generator.Generators;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Emitters;

using System.Linq;
using System.Text;
using Generator.Generators;
using Generator.Models;
using Generator.Utils;

/// <summary>Additional repository method bodies emitted into {Name}RepositoryImpl (EPIC 3).</summary>
internal static class RepositoryEmission
{
    public static void EmitAll(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        if (!entity.IsImmutable)
        {
            EmitBatchMethodOverrides(sb, entity, entityFqn, idType, provider);
            EmitDeleteAllByIdWithBatchHooks(sb, entity, idType);
        }

        MutatingMethodEmitter.EmitMutatingOverrides(sb, entity, entityFqn, idType, provider);

        if (entity.GlobalFilters.Any() && !entity.HasCompositeKey)
            GlobalFilterGenerator.EmitReadOverrides(sb, entity, entityFqn, idType);
        else if (entity.TenantIdColumn is not null && !entity.HasCompositeKey)
            TenancyGenerator.EmitReadOverrides(sb, entity, entityFqn, idType);
        else if (entity.SoftDeleteColumn is not null && !entity.HasCompositeKey)
            SoftDeleteReadOverrideGenerator.EmitReadOverrides(sb, entity, entityFqn, idType);
        else if (entity.RequiresDbRow && !entity.HasCompositeKey)
            EntityQueryEmitter.EmitStandardReadOverrides(sb, entity, entityFqn, idType);

        if (entity.HasCompositeKey && entity.CompositeKey is not null)
            CompositeKeyGenerator.EmitOverrides(sb, entity, entityFqn, idType);

        if (entity.SecondaryTables.Any())
            MutatingMethodEmitter.EmitDeleteAllByIdSecondaryFirst(sb, entity, idType);
        else if (entity.TenantIdColumn is not null && !entity.HasBatchLifecycle && !entity.HasCompositeKey)
            TenancyGenerator.EmitDeleteAllByIdOverride(sb, entity, idType);

        if (entity.IsImmutable)
            EmitImmutableBatchOverrides(sb, entityFqn, idType);
    }

    public static void EmitConstructorExtras(StringBuilder sb, EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels, string provider)
    {
        if (EntityNeedsRuntimeOptions(entity, allModels, provider))
        {
            sb.AppendLine("    private readonly DapperX.Abstractions.Configuration.IDapperXOptions? _options;");
            sb.AppendLine();
        }

        var hasAuditing = entity.Auditing?.CreatedByProperty is not null
            || entity.Auditing?.LastModifiedByProperty is not null
            || entity.Auditing?.CreatedDateProperty is not null
            || entity.Auditing?.LastModifiedDateProperty is not null;
        if (hasAuditing)
        {
            sb.AppendLine("    private readonly DapperX.Abstractions.Auditing.IAuditingProvider? _auditingProvider;");
            sb.AppendLine();
        }

        if (entity.TenantIdColumn is not null)
        {
            sb.AppendLine("    private readonly DapperX.Abstractions.Tenancy.ITenantProvider? _tenantProvider;");
            sb.AppendLine();
        }

        if (entity.Sequence is not null)
        {
            sb.AppendLine("    private readonly DapperX.Abstractions.Sequences.ISequenceAllocator? _sequenceAllocator;");
            sb.AppendLine();
        }

        if (entity.HasBatchLifecycle)
        {
            sb.AppendLine($"    private readonly {entity.ClassName}BatchLifecycleInvoker _batchLifecycle = new();");
            sb.AppendLine();
        }
    }

    public static void EmitConstructorAssignments(StringBuilder sb, EntityModel entity, IReadOnlyDictionary<string, EntityModel>? allModels = null, string? provider = null)
    {
        if (allModels is not null && provider is not null && EntityNeedsRuntimeOptions(entity, allModels, provider))
            sb.AppendLine("        _options = options;");
        else if (allModels is null && entity.GlobalFilters.Any())
            sb.AppendLine("        _options = options;");
        if (entity.Auditing is not null)
            sb.AppendLine("        _auditingProvider = auditingProvider;");
        if (entity.TenantIdColumn is not null)
            sb.AppendLine("        _tenantProvider = tenantProvider;");
        if (entity.Sequence is not null)
            sb.AppendLine("        _sequenceAllocator = sequenceAllocator;");
    }

    private static bool EntityNeedsRuntimeOptions(EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels, string provider)
        => !entity.IsImmutable;

    private static void EmitBatchMethodOverrides(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        EmitInsertMany(sb, entity, entityFqn, idType, provider);
        EmitUpdateMany(sb, entity, entityFqn);
        EmitDeleteMany(sb, entity, entityFqn);
    }

    private static void EmitInsertMany(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        var bulkEligible = BulkInsertEligibility.IsEligible(entity, provider);

        sb.AppendLine($"    public override async Task InsertManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null, int? bulkThreshold = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"InsertManyAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        sb.AppendLine("        var list = entities as ICollection<" + entityFqn + "> ?? entities.ToList();");
        EmitEffectiveBatchSize(sb);
        EmitBatchTrace(sb, "InsertSql");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePrePersistBatch(list);");

        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        if (idProp?.IdGenerationStrategy == "Sequence" && entity.Sequence is not null)
        {
            EmitSequenceInsertMany(sb, entity, idType);
        }
        else if (idProp?.IdGenerationStrategy == "Identity")
        {
            EmitIdentityInsertMany(sb, entity, entityFqn, idType, idProp);
        }
        else if (bulkEligible)
        {
            EmitBulkCapableAssignedInsertMany(sb, entity, entityFqn, provider);
        }
        else
        {
            EmitStandardBatchInsertMany(sb, entity);
        }

        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePostPersistBatch(list);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitEffectiveBatchSize(StringBuilder sb)
        => sb.AppendLine("        var effectiveBatchSize = batchSize ?? _options?.BatchSize ?? 1000;");

    private static void EmitBatchTrace(StringBuilder sb, string sqlProperty)
        => sb.AppendLine($"        DapperX.Runtime.Logging.SqlExecutionLogger.TryLogBatchTrace(DbExecutor.CreateLogContext(MethodName, Options, Provider), {sqlProperty}, list.Count, effectiveBatchSize);");

    private static void EmitEffectiveBulkThreshold(StringBuilder sb)
        => sb.AppendLine("        var effectiveBulkThreshold = bulkThreshold ?? _options?.BulkThreshold ?? 5000;");

    private static void EmitStandardBatchInsertMany(StringBuilder sb, EntityModel entity, string indent = "        ")
    {
        sb.AppendLine($"{indent}foreach (var chunk in BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine($"{indent}{{");
        EmitPrePersistLoop(sb, entity, indent + "    ");
        sb.AppendLine(entity.RequiresDbRow
            ? $"{indent}    await DbExecutor.ExecuteAsync(_connection, InsertSql, chunk.Select(BuildMutationParameters), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : $"{indent}    await DbExecutor.ExecuteAsync(_connection, InsertSql, chunk, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine($"{indent}    foreach (var entity in chunk)");
        sb.AppendLine($"{indent}    {{");
        if (entity.SecondaryTables.Any())
            MutatingMethodEmitter.EmitSecondaryInsertAfterPrimary(sb, entity);
        ElementCollectionLifecycleEmitter.EmitPersistLoadedOnInsert(sb, entity, indent: indent + "        ");
        sb.AppendLine($"{indent}        OnPostPersist(entity);");
        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
    }

    private static void EmitPrePersistLoop(StringBuilder sb, EntityModel entity, string indent = "            ")
    {
        sb.AppendLine($"{indent}foreach (var entity in list)");
        sb.AppendLine($"{indent}{{");
        if (entity.Auditing is not null)
            AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: false);
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        sb.AppendLine($"{indent}    OnPrePersist(entity);");
        sb.AppendLine($"{indent}}}");
    }

    private static void EmitSequenceInsertMany(StringBuilder sb, EntityModel entity, string idType)
    {
        var seqName = entity.Sequence!.SequenceName;
        var idProp = entity.Properties.First(p => p.IsId);
        sb.AppendLine("        foreach (var chunk in BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        if (entity.Auditing is not null)
            AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: false);
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        sb.AppendLine("                OnPrePersist(entity);");
        sb.AppendLine("                entity." + idProp.PropertyName + " = _sequenceAllocator is not null");
        sb.AppendLine($"                    ? ({idType})await _sequenceAllocator.NextAsync(\"{seqName}\", ct)");
        sb.AppendLine("                    : await DbExecutor.ExecuteScalarAsync<" + idType + ">(_connection, SequenceNextSql, transaction: transaction);");
        sb.AppendLine("            }");
        sb.AppendLine(entity.RequiresDbRow
            ? "            await DbExecutor.ExecuteAsync(_connection, InsertSql, chunk.Select(BuildMutationParameters), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : "            await DbExecutor.ExecuteAsync(_connection, InsertSql, chunk, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        if (entity.SecondaryTables.Any())
            MutatingMethodEmitter.EmitSecondaryInsertAfterPrimary(sb, entity);
        ElementCollectionLifecycleEmitter.EmitPersistLoadedOnInsert(sb, entity, indent: "                ");
        sb.AppendLine("                OnPostPersist(entity);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }

    private static void EmitIdentityInsertMany(StringBuilder sb, EntityModel entity, string entityFqn, string idType, PropertyModel idProp)
    {
        sb.AppendLine("        // Identity key backfill still requires per-entity scalar call.");
        sb.AppendLine("        foreach (var chunk in BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        if (entity.Auditing is not null)
            AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: false);
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        sb.AppendLine("                OnPrePersist(entity);");
        sb.AppendLine($"                entity.{idProp.PropertyName} = await DbExecutor.ExecuteScalarAsync<{idType}>(_connection, InsertSql, {EntityQueryEmitter.GetExecuteParameterExpression(entity)}, transaction);");
        if (entity.SecondaryTables.Any())
            MutatingMethodEmitter.EmitSecondaryInsertAfterPrimary(sb, entity);
        ElementCollectionLifecycleEmitter.EmitPersistLoadedOnInsert(sb, entity, indent: "                ");
        sb.AppendLine("                OnPostPersist(entity);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
    }

    private static void EmitBulkCapableAssignedInsertMany(StringBuilder sb, EntityModel entity, string entityFqn, string provider)
    {
        var executorType = BulkInsertGenerator.GetExecutorTypeName(provider);
        EmitEffectiveBulkThreshold(sb);
        sb.AppendLine("        if (list.Count >= effectiveBulkThreshold)");
        sb.AppendLine("        {");
        EmitPrePersistLoop(sb, entity);
        sb.AppendLine("            var bulkRows = list.Select(BuildBulkInsertRow).ToList();");
        sb.AppendLine("            await " + executorType + ".Instance.InsertAsync(new global::DapperX.Provider.Common.BulkInsertContext");
        sb.AppendLine("            {");
        sb.AppendLine("                Connection = _connection,");
        sb.AppendLine("                Transaction = transaction,");
        sb.AppendLine("                TableName = BulkInsertTableName,");
        sb.AppendLine("                ColumnNames = BulkInsertColumnNames,");
        sb.AppendLine("                ColumnTypes = BulkInsertColumnTypes,");
        sb.AppendLine("                Rows = bulkRows,");
        sb.AppendLine("            }, ct);");
        sb.AppendLine("            foreach (var entity in list)");
        sb.AppendLine("            {");
        ElementCollectionLifecycleEmitter.EmitPersistLoadedOnInsert(sb, entity, indent: "                ");
        sb.AppendLine("                OnPostPersist(entity);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        EmitStandardBatchInsertMany(sb, entity, indent: "            ");
        sb.AppendLine("        }");
    }

    private static void EmitUpdateMany(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        sb.AppendLine($"    public override async Task UpdateManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpdateManyAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        sb.AppendLine("        var list = entities as ICollection<" + entityFqn + "> ?? entities.ToList();");
        EmitEffectiveBatchSize(sb);
        EmitBatchTrace(sb, "UpdateSql");
        if (idProp is not null)
        {
            sb.AppendLine("        var conflictingKeys = new List<object>();");
            sb.AppendLine("        var totalAttempted = 0;");
            sb.AppendLine("        var totalAffected = 0;");
        }
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePreUpdateBatch(list);");
        sb.AppendLine("        foreach (var chunk in BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine("        {");
        if (idProp is not null)
            sb.AppendLine("            totalAttempted += chunk.Count;");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        if (entity.Auditing is not null)
            AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: true);
        sb.AppendLine("                OnPreUpdate(entity);");
        sb.AppendLine("            }");
        if (entity.TenantIdColumn is not null)
        {
            sb.AppendLine("            var chunkAffected = 0;");
            sb.AppendLine("            foreach (var entity in chunk)");
            sb.AppendLine("            {");
            sb.AppendLine($"                chunkAffected += await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            if (entity.SecondaryTables.Any())
                MutatingMethodEmitter.EmitSecondaryUpdateAfterPrimary(sb, entity);
            sb.AppendLine("            }");
            sb.AppendLine("            var affected = chunkAffected;");
        }
        else
        {
            sb.AppendLine($"            var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, chunk, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            if (entity.SecondaryTables.Any())
            {
                sb.AppendLine("            foreach (var entity in chunk)");
                sb.AppendLine("            {");
                MutatingMethodEmitter.EmitSecondaryUpdateAfterPrimary(sb, entity);
                sb.AppendLine("            }");
            }
        }
        if (idProp is not null)
        {
            sb.AppendLine("            totalAffected += affected;");
            sb.AppendLine("            if (affected != chunk.Count)");
            sb.AppendLine("            {");
            sb.AppendLine("                foreach (var entity in chunk)");
            sb.AppendLine("                {");
            sb.AppendLine(entity.TenantIdColumn is not null
                ? $"                    var rowAffected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
                : $"                    var rowAffected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("                    if (rowAffected == 0)");
            sb.AppendLine($"                        conflictingKeys.Add((object?)entity.{idProp.PropertyName} ?? \"<null>\");");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine("            if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Update failed — record may have been modified.\");");
        }
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        ElementCollectionLifecycleEmitter.EmitReplaceLoadedOnUpdate(sb, entity, indent: "                ");
        sb.AppendLine("                OnPostUpdate(entity);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        if (idProp is not null)
        {
            sb.AppendLine("        if (totalAffected != totalAttempted)");
            sb.AppendLine("            throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"One or more updates failed due to optimistic concurrency conflicts.\", conflictingKeys);");
        }
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePostUpdateBatch(list);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteMany(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        var idProp = entity.Properties.FirstOrDefault(p => p.IsId);
        sb.AppendLine($"    public override async Task DeleteManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteManyAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        sb.AppendLine("        var list = entities as ICollection<" + entityFqn + "> ?? entities.ToList();");
        EmitEffectiveBatchSize(sb);
        EmitBatchTrace(sb, "DeleteSql");
        if (idProp is not null)
        {
            sb.AppendLine("        var conflictingKeys = new List<object>();");
            sb.AppendLine("        var totalAttempted = 0;");
            sb.AppendLine("        var totalAffected = 0;");
        }
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePreRemoveBatch(list);");
        sb.AppendLine("        foreach (var chunk in BatchChunker.Chunk(list, effectiveBatchSize))");
        sb.AppendLine("        {");
        if (idProp is not null)
            sb.AppendLine("            totalAttempted += chunk.Count;");
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("            {");
        sb.AppendLine("                OnPreRemove(entity);");
        if (entity.ElementCollections.Any())
            ElementCollectionLifecycleEmitter.EmitDeleteAllBeforeParentDelete(sb, entity, indent: "                ");
        sb.AppendLine("            }");
        if (entity.TenantIdColumn is not null)
        {
            sb.AppendLine("            var chunkAffected = 0;");
            sb.AppendLine("            foreach (var entity in chunk)");
            sb.AppendLine("            {");
            if (entity.SecondaryTables.Any())
                MutatingMethodEmitter.EmitSecondaryDeleteBeforePrimary(sb, entity);
            sb.AppendLine($"                chunkAffected += await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("            }");
            sb.AppendLine("            var affected = chunkAffected;");
        }
        else if (entity.SecondaryTables.Any())
        {
            sb.AppendLine("            var affected = 0;");
            sb.AppendLine("            foreach (var entity in chunk)");
            sb.AppendLine("            {");
            MutatingMethodEmitter.EmitSecondaryDeleteBeforePrimary(sb, entity);
            sb.AppendLine($"                affected += await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("            }");
        }
        else if (entity.ElementCollections.Any())
        {
            sb.AppendLine("            var affected = 0;");
            sb.AppendLine("            foreach (var entity in chunk)");
            sb.AppendLine("            {");
            sb.AppendLine($"                affected += await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine($"            var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, chunk, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        }
        if (idProp is not null)
        {
            sb.AppendLine("            totalAffected += affected;");
            sb.AppendLine("            if (affected != chunk.Count)");
            sb.AppendLine("            {");
            sb.AppendLine("                foreach (var entity in chunk)");
            sb.AppendLine("                {");
            sb.AppendLine(entity.TenantIdColumn is not null
                ? $"                    var rowAffected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
                : $"                    var rowAffected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
            sb.AppendLine("                    if (rowAffected == 0)");
            sb.AppendLine($"                        conflictingKeys.Add((object?)entity.{idProp.PropertyName} ?? \"<null>\");");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
        }
        else
        {
            sb.AppendLine("            if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        }
        sb.AppendLine("            foreach (var entity in chunk)");
        sb.AppendLine("                OnPostRemove(entity);");
        sb.AppendLine("        }");
        if (idProp is not null)
        {
            sb.AppendLine("        if (totalAffected != totalAttempted)");
            sb.AppendLine("            throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"One or more deletes failed due to optimistic concurrency conflicts.\", conflictingKeys);");
        }
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePostRemoveBatch(list);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAllByIdWithBatchHooks(StringBuilder sb, EntityModel entity, string idType)
    {
        if (entity.SecondaryTables.Any())
            return;
        if (!entity.HasBatchLifecycle && !entity.HasCompositeKey)
            return;
        if (entity.HasCompositeKey)
            return;

        sb.AppendLine($"    public override async Task DeleteAllByIdAsync(IEnumerable<{idType}> ids, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAllByIdAsync\";");
        if (entity.HasBatchLifecycle)
        {
            sb.AppendLine($"        var empty = Array.Empty<{entity.FullyQualifiedName}>();");
            sb.AppendLine("        _batchLifecycle.InvokePreRemoveBatch(empty);");
        }
        if (entity.TenantIdColumn is not null)
            sb.AppendLine("        var tenantId = _tenantProvider?.GetCurrentTenantId();");
        sb.AppendLine(entity.TenantIdColumn is not null
            ? $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdsSql")}, new {{ ids, tenantId }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdsSql")}, new {{ ids }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePostRemoveBatch(empty);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitImmutableBatchOverrides(StringBuilder sb, string entityFqn, string idType)
    {
        const string msg = "Entity is marked [Immutable].";
        sb.AppendLine($"    public override Task InsertManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null, int? bulkThreshold = null)");
        sb.AppendLine($"        => throw new NotSupportedException(\"{msg}\");");
        sb.AppendLine();
        sb.AppendLine($"    public override Task UpdateManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine($"        => throw new NotSupportedException(\"{msg}\");");
        sb.AppendLine();
        sb.AppendLine($"    public override Task DeleteManyAsync(IEnumerable<{entityFqn}> entities, IDbTransaction? transaction = null, CancellationToken ct = default, int? batchSize = null)");
        sb.AppendLine($"        => throw new NotSupportedException(\"{msg}\");");
        sb.AppendLine();
    }

    public static void EmitHardDeleteAsync(StringBuilder sb, string entityFqn, string hardDeleteSql, EntityModel entity)
    {
        var escaped = hardDeleteSql.Replace("\\", "\\\\").Replace("\"", "\\\"");
        sb.AppendLine($"    private const string HardDeleteSql = \"{escaped}\";");
        sb.AppendLine();
        sb.AppendLine($"    public async Task HardDeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"HardDeleteAsync\";");
        if (entity.TenantIdColumn is not null)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "HardDeleteSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        else
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "HardDeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
