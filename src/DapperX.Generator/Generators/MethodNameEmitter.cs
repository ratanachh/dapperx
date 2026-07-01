namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Models;

/// <summary>Emits MethodName compile-time literals on base repository methods not covered elsewhere.</summary>
internal static class MethodNameEmitter
{
    public static void EmitBaseMethodWrappers(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        if (entity.GlobalFilters.Any() || entity.TenantIdColumn is not null || entity.SoftDeleteColumn is not null)
            return;

        if (!entity.RequiresDbRow && !entity.HasCompositeKey)
        {
            EmitGetById(sb, entityFqn, idType);
            EmitGetAll(sb, entityFqn);
            EmitGetAllSort(sb, entityFqn);
            EmitGetAllPage(sb, entityFqn);
            EmitGetAllSortPage(sb, entityFqn);
            EmitGetAllSlice(sb, entityFqn);
            EmitGetAllSliceSort(sb, entityFqn);
            EmitFindAllById(sb, entityFqn, idType);
        }

        if (!entity.HasCompositeKey)
            EmitExists(sb, idType);
        EmitCount(sb);

        if (!entity.IsImmutable && !NeedsMutatingInsertOverride(entity))
            EmitInsert(sb, entityFqn);
        if (!entity.IsImmutable && !NeedsMutatingUpdateOverride(entity))
            EmitUpdate(sb, entityFqn);
        if (!entity.IsImmutable && !NeedsMutatingDeleteOverride(entity))
            EmitDelete(sb, entityFqn);
        if (!entity.IsImmutable && !entity.HasRemoveHooks && !NeedsMutatingDeleteByIdOverride(entity))
            EmitDeleteById(sb, idType);
        if (!entity.IsImmutable && !entity.HasBatchLifecycle && entity.TenantIdColumn is null && !entity.SecondaryTables.Any())
            EmitDeleteAllById(sb, entityFqn, idType);
    }

    private static bool NeedsMutatingInsertOverride(EntityModel entity)
        => entity.RequiresDbRow
            || entity.SecondaryTables.Any()
            || entity.Properties.Any(p => p.GeneratedTime is not null)
            || entity.Properties.Any(p => p.IsId && p.IdGenerationStrategy == "Identity")
            || entity.Auditing is not null
            || entity.TenantIdColumn is not null
            || entity.Relationships.Any(r => r.IsPrimaryKeyJoin)
            || entity.ElementCollections.Any();

    private static bool NeedsMutatingUpdateOverride(EntityModel entity)
        => entity.RequiresDbRow
            || entity.SecondaryTables.Any()
            || entity.Properties.Any(p => p.GeneratedTime == "Always")
            || entity.Auditing is not null
            || entity.TenantIdColumn is not null
            || entity.GlobalFilters.Any()
            || entity.ElementCollections.Any();

    private static bool NeedsMutatingDeleteOverride(EntityModel entity)
        => entity.SecondaryTables.Any()
            || entity.TenantIdColumn is not null
            || entity.GlobalFilters.Any()
            || entity.HasCompositeKey
            || (entity.ElementCollections.Any()
                && !entity.SecondaryTables.Any()
                && entity.TenantIdColumn is null
                && !entity.GlobalFilters.Any());

    private static bool NeedsMutatingDeleteByIdOverride(EntityModel entity)
        => entity.SecondaryTables.Any()
            || entity.TenantIdColumn is not null
            || entity.HasCompositeKey
            || (entity.ElementCollections.Any()
                && !entity.SecondaryTables.Any()
                && entity.TenantIdColumn is null
                && !entity.GlobalFilters.Any());

    private static void EmitGetById(StringBuilder sb, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetByIdAsync(id, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAll(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllAsync(includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSort(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync(Sort sort, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllAsync(sort, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllPage(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllAsync(pageable, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSortPage(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Sort sort, Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllAsync(sort, pageable, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSlice(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllSliceAsync(pageable, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSliceSort(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Sort sort, Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.GetAllSliceAsync(sort, pageable, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitFindAllById(StringBuilder sb, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> FindAllByIdAsync(IEnumerable<{idType}> ids, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.FindAllByIdAsync(ids, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitExists(StringBuilder sb, string idType)
    {
        sb.AppendLine($"    public override async Task<bool> ExistsByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.ExistsByIdAsync(id, includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitCount(StringBuilder sb)
    {
        sb.AppendLine($"    public override async Task<long> CountAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        return await base.CountAsync(includeDeleted, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitInsert(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task InsertAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.InsertAsync(entity, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitUpdate(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task UpdateAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.UpdateAsync(entity, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDelete(StringBuilder sb, string entityFqn)
    {
        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.DeleteAsync(entity, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteById(StringBuilder sb, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.DeleteByIdAsync(id, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitDeleteAllById(StringBuilder sb, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task DeleteAllByIdAsync(IEnumerable<{idType}> ids, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        await base.DeleteAllByIdAsync(ids, transaction, ct);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
