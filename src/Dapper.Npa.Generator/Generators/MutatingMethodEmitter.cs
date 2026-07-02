using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Emitters;
using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Generators;

using System.Linq;
using System.Text;
using Generator.Builders;
using Generator.Emitters;
using Generator.Models;
using Generator.Utils;

/// <summary>Unified InsertAsync/UpdateAsync/DeleteAsync emission (auditing, tenancy, identity, secondary, generated).</summary>
internal static class MutatingMethodEmitter
{
    public static void EmitMutatingOverrides(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        if (entity.IsImmutable)
            return;

        EmitSupportingMembers(sb, entity, entityFqn, idType, provider);

        if (NeedsInsertOverride(entity))
            EmitInsertAsync(sb, entity, entityFqn, idType, provider);

        if (NeedsUpdateOverride(entity))
            EmitUpdateAsync(sb, entity, entityFqn, provider);

        if (entity.SecondaryTables.Any())
        {
            EmitDeleteAsyncSecondaryFirst(sb, entity, entityFqn, entity.HasRemoveHooks);
            EmitDeleteByIdSecondaryFirst(sb, entity, idType);
            if (entity.TenantIdColumn is not null && entity.HasRemoveHooks)
                TenancyGenerator.EmitDeleteByIdOverride(sb, entity, idType);
        }
        else if (entity.TenantIdColumn is not null)
        {
            TenancyGenerator.EmitDeleteAsyncOverride(sb, entity, entityFqn);
            if (!entity.HasRemoveHooks)
                TenancyGenerator.EmitDeleteByIdOverride(sb, entity, idType);
        }
        else if (entity.GlobalFilters.Any())
        {
            EmitDeleteAsyncWithFilters(sb, entity, entityFqn, entity.HasRemoveHooks);
            if (!entity.HasRemoveHooks)
                EmitDeleteByIdWithFilters(sb, entity, idType);
        }
        else if (entity.ElementCollections.Any())
        {
            EmitDeleteAsyncWithElementCollections(sb, entity, entityFqn, entity.HasRemoveHooks);
            EmitDeleteByIdWithElementCollections(sb, entity, idType);
        }
    }

    private static bool NeedsInsertOverride(EntityModel entity)
        => entity.RequiresDbRow
            || entity.SecondaryTables.Any()
            || entity.Properties.Any(p => p.GeneratedTime is not null)
            || entity.Properties.Any(p => p.IsId && p.IdGenerationStrategy == "Identity")
            || entity.Auditing is not null
            || entity.TenantIdColumn is not null
            || entity.Relationships.Any(r => r.IsPrimaryKeyJoin)
            || entity.ElementCollections.Any();

    private static bool NeedsUpdateOverride(EntityModel entity)
        => entity.RequiresDbRow
            || entity.SecondaryTables.Any()
            || entity.Properties.Any(p => p.GeneratedTime == "Always")
            || entity.Auditing is not null
            || entity.TenantIdColumn is not null
            || entity.GlobalFilters.Any()
            || entity.ElementCollections.Any();

    private static void EmitSupportingMembers(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        var generatedProps = entity.Properties.Where(p => p.GeneratedTime is not null).ToList();
        if (GeneratedColumnSqlBuilder.NeedsReSelectConstant(entity, provider))
        {
            var reSelectSql = GeneratedColumnSqlBuilder.BuildReSelectSql(entity);
            sb.AppendLine($"    private static readonly string GeneratedColumnsReSelectSql = \"{Escape(reSelectSql)}\";");
            sb.AppendLine();
            EmitCopyGeneratedColumnsHelper(sb, entity, generatedProps);
            EmitApplyGeneratedColumnsFromRowHelper(sb, entity);
        }

        if (GeneratedColumnSqlBuilder.HasGeneratedProperties(entity)
            && GeneratedColumnSqlBuilder.UsesInlineInsertOutput(provider))
        {
            GeneratedColumnEmitter.EmitInsertFetchRowType(sb, entity, idType);
            GeneratedColumnEmitter.EmitApplyInsertFetchHelper(sb, entity, entityFqn, idType);
        }

        foreach (var st in entity.SecondaryTables)
        {
            var insertSql = SecondaryTableGenerator.BuildSecondaryInsertSql(entity, st);
            var updateSql = SecondaryTableGenerator.BuildSecondaryUpdateSql(entity, st);
            var deleteSql = SecondaryTableGenerator.BuildSecondaryDeleteSql(st);
            var deleteByIdsSql = SecondaryTableGenerator.BuildSecondaryDeleteByIdsSql(st, provider);
            var key = SecondaryTableGenerator.SanitizeTableKey(st.TableName);
            sb.AppendLine($"    private static readonly string SecondaryInsert_{key} = \"{Escape(insertSql)}\";");
            sb.AppendLine($"    private static readonly string SecondaryUpdate_{key} = \"{Escape(updateSql)}\";");
            sb.AppendLine($"    private static readonly string SecondaryDelete_{key} = \"{Escape(deleteSql)}\";");
            sb.AppendLine($"    private static readonly string SecondaryDeleteByIds_{key} = \"{Escape(deleteByIdsSql)}\";");
        }

        if (entity.SecondaryTables.Any())
            sb.AppendLine();

    }

    private static void EmitInsertAsync(StringBuilder sb, EntityModel entity, string entityFqn, string idType, string provider)
    {
        var idProp = entity.Properties.First(p => p.IsId);
        var useIdentity = idProp.IdGenerationStrategy == "Identity";
        var useSequence = idProp.IdGenerationStrategy == "Sequence" && entity.Sequence is not null;
        var hasGenerated = GeneratedColumnSqlBuilder.HasGeneratedProperties(entity);
        var inlineInsertFetch = hasGenerated && GeneratedColumnSqlBuilder.UsesInlineInsertOutput(provider);

        sb.AppendLine($"    public override async Task InsertAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"InsertAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        if (!useIdentity && !useSequence)
            PrimaryKeyJoinColumnGenerator.EmitAssignBeforeInsert(sb, entity);
        TenancyGenerator.EmitApplyTenantIdCall(sb, entity);
        AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: false);
        sb.AppendLine("        OnPrePersist(entity);");

        if (useSequence)
        {
            var seqName = entity.Sequence!.SequenceName;
            sb.AppendLine("        entity." + idProp.PropertyName + " = _sequenceAllocator is not null");
            sb.AppendLine($"            ? ({idType})await _sequenceAllocator.NextAsync(\"{seqName}\", ct)");
            sb.AppendLine($"            : await DbExecutor.ExecuteScalarAsync<{idType}>(_connection, SequenceNextSql, transaction: transaction);");
            PrimaryKeyJoinColumnGenerator.EmitAssignAfterParentId(sb, entity);
        }

        var insertParams = EntityQueryEmitter.GetExecuteParameterExpression(entity);
        if (inlineInsertFetch)
        {
            sb.AppendLine($"        var __insertFetch = await DbExecutor.QueryFirstOrDefaultAsync<GeneratedInsertFetchRow>(_connection, InsertSql, {insertParams}, transaction{DbExecutorEmission.LogContextSuffix});");
            sb.AppendLine("        if (__insertFetch is not null)");
            sb.AppendLine("            ApplyGeneratedInsertFetch(entity, __insertFetch);");
            PrimaryKeyJoinColumnGenerator.EmitAssignAfterParentId(sb, entity);
        }
        else if (useIdentity)
        {
            sb.AppendLine($"        var newId = await DbExecutor.ExecuteScalarAsync<{idType}>(_connection, InsertSql, {insertParams}, transaction{DbExecutorEmission.LogContextSuffix});");
            sb.AppendLine($"        entity.{idProp.PropertyName} = newId;");
            PrimaryKeyJoinColumnGenerator.EmitAssignAfterParentId(sb, entity);
        }
        else
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, InsertSql, {insertParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");

        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryInsert_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");

        if (!inlineInsertFetch)
            EmitGeneratedColumnReSelect(sb, entity, entityFqn, "entity", provider);

        ElementCollectionLifecycleEmitter.EmitPersistLoadedOnInsert(sb, entity);

        sb.AppendLine("        OnPostPersist(entity);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUpdateAsync(StringBuilder sb, EntityModel entity, string entityFqn, string provider)
    {
        sb.AppendLine($"    public override async Task UpdateAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"UpdateAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        AuditingGenerator.EmitPopulateBeforePersist(sb, entity, isUpdate: true);
        sb.AppendLine("        OnPreUpdate(entity);");
        var updateParams = entity.TenantIdColumn is not null
            ? "WithTenantParams(entity)"
            : EntityQueryEmitter.GetExecuteParameterExpression(entity);
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "UpdateSql")}, {updateParams}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new Dapper.Npa.Abstractions.Exceptions.ConcurrencyException(\"Update failed — record may have been modified.\");");

        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryUpdate_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");

        EmitGeneratedColumnReSelect(sb, entity, entityFqn, "entity", provider, onlyAlways: true);

        ElementCollectionLifecycleEmitter.EmitReplaceLoadedOnUpdate(sb, entity);

        sb.AppendLine("        OnPostUpdate(entity);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAsyncSecondaryFirst(StringBuilder sb, EntityModel entity, string entityFqn, bool withHooks)
    {
        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        if (withHooks)
            sb.AppendLine("        OnPreRemove(entity);");
        ElementCollectionLifecycleEmitter.EmitDeleteAllBeforeParentDelete(sb, entity);
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryDelete_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine(entity.TenantIdColumn is not null
            ? $"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : $"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new Dapper.Npa.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        if (withHooks)
            sb.AppendLine("        OnPostRemove(entity);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAsyncWithFilters(StringBuilder sb, EntityModel entity, string entityFqn, bool withHooks)
    {
        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAsync\";");
        if (withHooks)
            sb.AppendLine("        OnPreRemove(entity);");
        ElementCollectionLifecycleEmitter.EmitDeleteAllBeforeParentDelete(sb, entity);
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new Dapper.Npa.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        if (withHooks)
            sb.AppendLine("        OnPostRemove(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAsyncWithElementCollections(StringBuilder sb, EntityModel entity, string entityFqn, bool withHooks)
    {
        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAsync\";");
        if (withHooks)
            sb.AppendLine("        OnPreRemove(entity);");
        ElementCollectionLifecycleEmitter.EmitDeleteAllBeforeParentDelete(sb, entity);
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, entity, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new Dapper.Npa.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        if (withHooks)
            sb.AppendLine("        OnPostRemove(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteByIdWithElementCollections(StringBuilder sb, EntityModel entity, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteByIdAsync\";");
        ElementCollectionLifecycleEmitter.EmitDeleteAllByParentId(sb, entity, "id");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, new {{ {ParameterBindingHelper.IdAssignment(entity)} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteByIdWithFilters(StringBuilder sb, EntityModel entity, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteByIdAsync\";");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, new {{ {ParameterBindingHelper.IdAssignment(entity)} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteByIdSecondaryFirst(StringBuilder sb, EntityModel entity, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteByIdAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        ElementCollectionLifecycleEmitter.EmitDeleteAllByParentId(sb, entity, "id");
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryDelete_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, new {{ {ParameterBindingHelper.IdAssignment(entity)} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine(entity.TenantIdColumn is not null
            ? $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, new {{ {ParameterBindingHelper.IdAssignment(entity)}, tenantId = _tenantProvider?.GetCurrentTenantId() }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, new {{ {ParameterBindingHelper.IdAssignment(entity)} }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitDeleteAllByIdSecondaryFirst(StringBuilder sb, EntityModel entity, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteAllByIdAsync(IEnumerable<{idType}> ids, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAllByIdAsync\";");
        SecondaryTableTransactionEmitter.EmitScopeStart(sb, entity);
        if (entity.HasBatchLifecycle)
        {
            sb.AppendLine($"        var empty = Array.Empty<{entity.FullyQualifiedName}>();");
            sb.AppendLine("        _batchLifecycle.InvokePreRemoveBatch(empty);");
        }
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, SecondaryDeleteByIds_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, new {{ ids }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        if (entity.TenantIdColumn is not null)
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdsSql")}, new {{ ids, tenantId = _tenantProvider?.GetCurrentTenantId() }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        else
            sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdsSql")}, new {{ ids }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        if (entity.HasBatchLifecycle)
            sb.AppendLine("        _batchLifecycle.InvokePostRemoveBatch(empty);");
        SecondaryTableTransactionEmitter.EmitScopeEnd(sb, entity);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitSecondaryInsertAfterPrimary(StringBuilder sb, EntityModel entity, string entityVar = "entity")
    {
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"            await DbExecutor.ExecuteAsync(_connection, SecondaryInsert_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, {entityVar}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
    }

    public static void EmitSecondaryUpdateAfterPrimary(StringBuilder sb, EntityModel entity, string entityVar = "entity")
    {
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"            await DbExecutor.ExecuteAsync(_connection, SecondaryUpdate_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, {entityVar}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
    }

    public static void EmitSecondaryDeleteBeforePrimary(StringBuilder sb, EntityModel entity, string entityVar = "entity")
    {
        foreach (var st in entity.SecondaryTables)
            sb.AppendLine($"            await DbExecutor.ExecuteAsync(_connection, SecondaryDelete_{SecondaryTableGenerator.SanitizeTableKey(st.TableName)}, {entityVar}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
    }

    private static void EmitGeneratedColumnReSelect(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string entityVar,
        string provider,
        bool onlyAlways = false)
    {
        var generatedProps = entity.Properties
            .Where(p => p.GeneratedTime is not null && (!onlyAlways || p.GeneratedTime == "Always"))
            .ToList();
        if (generatedProps.Count == 0)
            return;

        var reSelectParams = EntityQueryEmitter.GetExecuteParameterExpression(entity, entityVar);
        if (entity.RequiresDbRow)
        {
            var rowType = EmbeddedMappingEmitter.DbRowTypeName(entity);
            sb.AppendLine($"        var __genRow = await DbExecutor.QueryFirstOrDefaultAsync<{rowType}>(_connection, GeneratedColumnsReSelectSql, {reSelectParams}, transaction);");
            sb.AppendLine("        if (__genRow is not null)");
            sb.AppendLine($"            ApplyGeneratedColumnsFromRow({entityVar}, __genRow);");
        }
        else
        {
            sb.AppendLine($"        var generated = await DbExecutor.QueryFirstOrDefaultAsync<{entityFqn}>(_connection, GeneratedColumnsReSelectSql, {reSelectParams}, transaction);");
            sb.AppendLine("        if (generated is not null)");
            sb.AppendLine($"            CopyGeneratedColumns({entityVar}, generated);");
        }
    }

    public static void EmitApplyGeneratedColumnsFromRowHelper(StringBuilder sb, EntityModel entity)
    {
        var generatedProps = entity.Properties.Where(p => p.GeneratedTime is not null).ToList();
        if (generatedProps.Count == 0 || !entity.RequiresDbRow)
            return;

        var rowType = EmbeddedMappingEmitter.DbRowTypeName(entity);
        sb.AppendLine($"    private static void ApplyGeneratedColumnsFromRow({entity.FullyQualifiedName} target, {rowType} row)");
        sb.AppendLine("    {");
        foreach (var p in generatedProps)
        {
            if (p.ConverterTypeName is not null)
                sb.AppendLine($"        target.{p.PropertyName} = _conv_{p.PropertyName}.ToProperty(row.{p.PropertyName});");
            else
                sb.AppendLine($"        target.{p.PropertyName} = row.{p.PropertyName};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitCopyGeneratedColumnsHelper(StringBuilder sb, EntityModel entity, List<PropertyModel> generatedProps)
    {
        sb.AppendLine($"    private static void CopyGeneratedColumns({entity.FullyQualifiedName} target, {entity.FullyQualifiedName} source)");
        sb.AppendLine("    {");
        foreach (var p in generatedProps)
        {
            if (p.ConverterTypeName is not null)
                sb.AppendLine($"        target.{p.PropertyName} = _conv_{p.PropertyName}.ToProperty(source.{p.PropertyName});");
            else
                sb.AppendLine($"        target.{p.PropertyName} = source.{p.PropertyName};");
        }
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
