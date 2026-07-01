namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

/// <summary>Tenant filter SQL and runtime parameter wiring for multi-tenancy (Requirements §33).</summary>
internal static class TenancyGenerator
{
    public static string? BuildTenantWherePredicate(EntityModel entity)
        => entity.TenantIdColumn is null ? null : $"{entity.TenantIdColumn} = @tenantId";

    public static string AppendTenantToWhere(string whereClause, EntityModel entity)
    {
        var predicate = BuildTenantWherePredicate(entity);
        if (predicate is null)
            return whereClause;

        return whereClause.Contains("WHERE ", StringComparison.Ordinal)
            ? $"{whereClause} AND {predicate}"
            : $"WHERE {predicate}";
    }

    public static void EmitApplyTenantIdMethod(StringBuilder sb, EntityModel entity)
    {
        var tenantProp = FindTenantProperty(entity);
        if (tenantProp is null)
            return;

        sb.AppendLine("    private void ApplyTenantIdFromProvider(" + entity.FullyQualifiedName + " entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_tenantProvider is null)");
        sb.AppendLine("            return;");
        sb.AppendLine($"        entity.{tenantProp.PropertyName} = ({tenantProp.ClrTypeName})_tenantProvider.GetCurrentTenantId()!;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitWithTenantParamsMethod(StringBuilder sb, EntityModel entity)
    {
        if (entity.TenantIdColumn is null)
            return;

        sb.AppendLine($"    private Dapper.DynamicParameters WithTenantParams({entity.FullyQualifiedName} entity)");
        sb.AppendLine("    {");
        sb.AppendLine(entity.RequiresDbRow
            ? "        var parameters = new Dapper.DynamicParameters(BuildMutationParameters(entity));"
            : "        var parameters = new Dapper.DynamicParameters(entity);");
        sb.AppendLine("        parameters.Add(\"tenantId\", _tenantProvider?.GetCurrentTenantId());");
        sb.AppendLine("        return parameters;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitApplyTenantIdCall(StringBuilder sb, EntityModel entity)
    {
        if (FindTenantProperty(entity) is null)
            return;
        sb.AppendLine("        ApplyTenantIdFromProvider(entity);");
    }

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

    public static void EmitDeleteAsyncOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        if (entity.TenantIdColumn is null)
            return;

        sb.AppendLine($"    public override async Task DeleteAsync({entityFqn} entity, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAsync\";");
        sb.AppendLine("        OnPreRemove(entity);");
        sb.AppendLine($"        var affected = await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteSql")}, WithTenantParams(entity), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("        if (affected == 0) throw new DapperX.Abstractions.Exceptions.ConcurrencyException(\"Delete failed — record may have been modified.\");");
        sb.AppendLine("        OnPostRemove(entity);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitDeleteByIdOverride(StringBuilder sb, EntityModel entity, string idType)
    {
        if (entity.TenantIdColumn is null)
            return;

        sb.AppendLine($"    public override async Task DeleteByIdAsync({idType} id, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteByIdAsync\";");
        sb.AppendLine("        var tenantId = _tenantProvider?.GetCurrentTenantId();");
        sb.AppendLine(entity.GlobalFilters.Any()
            ? $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, BuildReadParameters(new {{ {ParameterBindingHelper.IdAssignment(entity)} }}), transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));"
            : $"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdSql")}, new {{ {ParameterBindingHelper.IdAssignment(entity)}, tenantId }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static void EmitDeleteAllByIdOverride(StringBuilder sb, EntityModel entity, string idType)
    {
        if (entity.TenantIdColumn is null)
            return;

        sb.AppendLine($"    public override async Task DeleteAllByIdAsync(IEnumerable<{idType}> ids, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"DeleteAllByIdAsync\";");
        sb.AppendLine("        var tenantId = _tenantProvider?.GetCurrentTenantId();");
        sb.AppendLine($"        await DbExecutor.ExecuteAsync(_connection, {GlobalFilterGenerator.FilteredSql(entity, "DeleteByIdsSql")}, new {{ ids, tenantId }}, transaction, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    public static PropertyModel? FindTenantProperty(EntityModel entity)
    {
        if (entity.TenantIdColumn is null)
            return null;
        return entity.Properties.FirstOrDefault(p =>
            string.Equals(p.ColumnName, entity.TenantIdColumn, StringComparison.OrdinalIgnoreCase));
    }

    private static string TenantParamSuffix(EntityModel entity)
        => entity.TenantIdColumn is null ? string.Empty : ", tenantId = _tenantProvider?.GetCurrentTenantId()";

    private static string TenantOnlyParams(EntityModel entity)
        => entity.TenantIdColumn is null ? "null" : "new { tenantId = _tenantProvider?.GetCurrentTenantId() }";

    private static void EmitGetByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetByIdAsync\";");
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectById");
        var sqlExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectByIdSql";
        EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn,
            sqlExpr, $"new {{ {ParameterBindingHelper.IdAssignment(entity)}{TenantParamSuffix(entity)} }}", "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAll");
        var sqlExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectAllSql";
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            sqlExpr, TenantOnlyParams(entity), "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAll");
        var selectExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectAllSql";
        sb.AppendLine($"        var sql = {selectExpr} + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            "sql", TenantOnlyParams(entity), "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "CountPage", "countBase");
        var countExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "countBase" : "CountPageSql";
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, {countExpr}, {TenantOnlyParams(entity)}, transaction{DbExecutorEmission.LogContextSuffix});");
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllPage", "pageBase");
        var pageExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "pageBase" : "SelectAllPageSql";
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            pageExpr, $"new {{ offset = pageable.Offset, pageSize = pageable.PageSize{TenantParamSuffix(entity)} }}", "transaction", "content", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "CountPage", "countBase");
        var countExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "countBase" : "CountPageSql";
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, {countExpr}, {TenantOnlyParams(entity)}, transaction{DbExecutorEmission.LogContextSuffix});");
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllPage", "pageBase");
        var pageBase = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "pageBase" : "SelectAllPageSql";
        sb.AppendLine($"        var pageSql = {pageBase} + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageSql", $"new {{ offset = pageable.Offset, pageSize = pageable.PageSize{TenantParamSuffix(entity)} }}", "transaction", "content", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllSlice");
        var sliceExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectAllSliceSql";
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            sliceExpr, $"new {{ offset = pageable.Offset, sliceSize = pageable.PageSize + 1{TenantParamSuffix(entity)} }}", "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectAllSlice");
        var sliceBase = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectAllSliceSql";
        sb.AppendLine($"        var sql = {sliceBase} + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", $"new {{ offset = pageable.Offset, sliceSize = pageable.PageSize + 1{TenantParamSuffix(entity)} }}", "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "SelectByIds");
        var sqlExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "SelectByIdsSql";
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            sqlExpr, $"new {{ ids{TenantParamSuffix(entity)} }}", "transaction", emitLogContext: true);
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
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "Exists");
        var sqlExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "ExistsSql";
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, {sqlExpr}, new {{ {ParameterBindingHelper.IdAssignment(entity)}{TenantParamSuffix(entity)} }}, transaction{DbExecutorEmission.LogContextSuffix}) == 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitCountOverride(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine($"    public override async Task<long> CountAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"CountAsync\";");
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, "Count");
        var sqlExpr = SoftDeleteBypassHelper.HasSoftDelete(entity) ? "baseSql" : "CountSql";
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<long>(_connection, {sqlExpr}, {TenantOnlyParams(entity)}, transaction{DbExecutorEmission.LogContextSuffix});");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
