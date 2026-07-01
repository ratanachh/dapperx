namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

/// <summary>Read overrides with <c>includeDeleted</c> for entities with <c>[SoftDelete]</c> and no tenancy/global-filter overrides.</summary>
internal static class SoftDeleteReadOverrideGenerator
{
    public static void EmitReadOverrides(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        EmitGetByIdOverride(sb, entity, entityFqn, idType);
        EmitGetAllOverride(sb, entity, entityFqn);
        EmitGetAllSortOverride(sb, entity, entityFqn);
        EmitGetAllPageOverride(sb, entity, entityFqn);
        EmitGetAllSortPageOverride(sb, entity, entityFqn);
        EmitGetAllSliceOverride(sb, entity, entityFqn);
        EmitGetAllSliceSortOverride(sb, entity, entityFqn);
        EmitFindAllByIdOverride(sb, entity, entityFqn, idType);
        EmitExistsOverride(sb, entity, idType);
        EmitCountOverride(sb, entity);
    }

    private static void EmitGetByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetByIdAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectById");
        EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn, "baseSql", "new { Id = id }", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(result);");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAll");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn, "baseSql", "null", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSortOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync(Sort sort, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAll");
        sb.AppendLine("        var sql = baseSql + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn, "sql", "null", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllPageOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "CountPage", "countBase");
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countBase, null, transaction{DbExecutorEmission.LogContextSuffix});");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllPage", "pageBase");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageBase", "new { offset = pageable.Offset, pageSize = pageable.PageSize }", "transaction", "content", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(content);");
        sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSortPageOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Sort sort, Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "CountPage", "countBase");
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countBase, null, transaction{DbExecutorEmission.LogContextSuffix});");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllPage", "pageBase");
        sb.AppendLine("        var sql = pageBase + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", "new { offset = pageable.Offset, pageSize = pageable.PageSize }", "transaction", "content", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(content);");
        sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSliceOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllSliceAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllSlice");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "baseSql", "new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine($"        return new Slice<{entityFqn}>(results, pageable.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSliceSortOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Sort sort, Pageable pageable, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetAllSliceAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllSlice");
        sb.AppendLine("        var sql = baseSql + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", "new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine($"        return new Slice<{entityFqn}>(results, pageable.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitFindAllByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> FindAllByIdAsync(IEnumerable<{idType}> ids, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"FindAllByIdAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectByIds");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn, "baseSql", "new { ids }", "transaction", emitLogContext: true);
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitExistsOverride(StringBuilder sb, EntityModel entity, string idType)
    {
        sb.AppendLine($"    public override async Task<bool> ExistsByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"ExistsByIdAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "Exists");
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, baseSql, new {{ Id = id }}, transaction{DbExecutorEmission.LogContextSuffix}) == 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitCountOverride(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine($"    public override async Task<long> CountAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"CountAsync\";");
        SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "Count");
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<long>(_connection, baseSql, null, transaction{DbExecutorEmission.LogContextSuffix});");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
