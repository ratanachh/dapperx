namespace DapperX.Generator.Generators;

using System.Linq;
using System.Text;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;

/// <summary>Emits runtime append of compile-time FILTER_* constants.</summary>
internal static class GlobalFilterGenerator
{
    public static bool EntityNeedsOptions(EntityModel entity, IReadOnlyDictionary<string, EntityModel> allModels)
        => entity.GlobalFilters.Any() || GetLazyChildrenWithFilters(entity, allModels).Any();

    public static IReadOnlyList<EntityModel> GetLazyChildrenWithFilters(
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var result = new List<EntityModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rel in entity.Relationships.Where(r => r.IsBatchLoadable && !string.IsNullOrEmpty(r.ChildEntityFqn)))
        {
            if (!TryGetChildModel(rel.ChildEntityFqn!, allModels, out var child))
                continue;
            if (!child.GlobalFilters.Any() || !seen.Add(child.FullyQualifiedName))
                continue;
            result.Add(child);
        }
        return result;
    }

    public static string FilteredSql(EntityModel entity, string sqlProperty)
        => entity.GlobalFilters.Any() ? $"ApplyGlobalFilters({sqlProperty})" : sqlProperty;

    public static string FilteredChildSql(EntityModel child, string sqlProperty)
        => $"Apply{child.ClassName}GlobalFilters({sqlProperty})";

    public static void EmitReadOverrides(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        EmitReadParametersBuilder(sb, entity);
        EmitApplyFiltersMethod(sb, entity);
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

    public static void EmitApplyFiltersOnly(StringBuilder sb, EntityModel entity)
        => EmitApplyFiltersMethod(sb, entity);

    public static void EmitChildApplyFiltersMethods(
        StringBuilder sb,
        EntityModel parent,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        foreach (var child in GetLazyChildrenWithFilters(parent, allModels))
            EmitApplyFiltersMethodForChild(sb, child);
    }

    public static void EmitCpqlApplyGlobalFilters(StringBuilder sb, EntityModel entity)
    {
        if (!entity.GlobalFilters.Any())
            return;
        sb.AppendLine("        sql = ApplyGlobalFilters(sql);");
    }

    public static void EmitCpqlApplyGlobalFiltersCount(StringBuilder sb, EntityModel entity, string variableName = "countSql")
    {
        if (!entity.GlobalFilters.Any())
            return;
        sb.AppendLine($"        {variableName} = ApplyGlobalFilters({variableName});");
    }

    private static void EmitApplyFiltersMethod(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine("    private string ApplyGlobalFilters(string sql)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_options is null)");
        sb.AppendLine("            return sql;");
        sb.AppendLine("        var hasActiveFilter = false;");
        foreach (var gf in entity.GlobalFilters)
        {
            sb.AppendLine($"        if (_options.IsFilterActive(\"{gf.Name}\"))");
            sb.AppendLine("            hasActiveFilter = true;");
        }
        sb.AppendLine("        if (hasActiveFilter && !sql.Contains(\"WHERE\", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("            sql += \" WHERE 1=1\";");
        foreach (var gf in entity.GlobalFilters)
        {
            sb.AppendLine($"        if (_options.IsFilterActive(\"{gf.Name}\"))");
            sb.AppendLine($"            sql += {gf.ConstantName};");
        }
        sb.AppendLine("        return sql;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitApplyFiltersMethodForChild(StringBuilder sb, EntityModel child)
    {
        var methodName = $"Apply{child.ClassName}GlobalFilters";
        sb.AppendLine($"    private string {methodName}(string sql)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (_options is null)");
        sb.AppendLine("            return sql;");
        sb.AppendLine("        var hasActiveFilter = false;");
        foreach (var gf in child.GlobalFilters)
        {
            sb.AppendLine($"        if (_options.IsFilterActive(\"{gf.Name}\"))");
            sb.AppendLine("            hasActiveFilter = true;");
        }
        sb.AppendLine("        if (hasActiveFilter && !sql.Contains(\"WHERE\", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine("            sql += \" WHERE 1=1\";");
        foreach (var gf in child.GlobalFilters)
        {
            sb.AppendLine($"        if (_options.IsFilterActive(\"{gf.Name}\"))");
            sb.AppendLine($"            sql += {child.ClassName}Filters.{gf.ConstantName};");
        }
        sb.AppendLine("        return sql;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string ReadParams(EntityModel entity, string extraFields)
    {
        if (extraFields == "id")
            extraFields = Utils.ParameterBindingHelper.IdAssignment(entity);
        return string.IsNullOrEmpty(extraFields)
            ? "BuildReadParameters()"
            : $"BuildReadParameters(new {{ {extraFields} }})";
    }

    private static void EmitReadParametersBuilder(StringBuilder sb, EntityModel entity)
    {
        if (!entity.GlobalFilters.Any() && entity.TenantIdColumn is null)
            return;

        sb.AppendLine("    private Dapper.DynamicParameters BuildReadParameters(object? extra = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        var p = new Dapper.DynamicParameters(extra);");
        if (entity.TenantIdColumn is not null)
        {
            sb.AppendLine("        if (_tenantProvider is not null)");
            sb.AppendLine("            p.Add(\"tenantId\", _tenantProvider.GetCurrentTenantId());");
        }
        foreach (var gf in entity.GlobalFilters)
        {
            sb.AppendLine($"        if (_options?.IsFilterActive(\"{gf.Name}\") == true)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var __filterParams = _options.GetFilterParameters(\"{gf.Name}\");");
            sb.AppendLine("            if (__filterParams is not null) p.AddDynamicParams(__filterParams);");
            sb.AppendLine("        }");
        }
        sb.AppendLine("        return p;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitSoftDeleteBase(StringBuilder sb, EntityModel entity, string prefix, string varName = "baseSql")
    {
        if (SoftDeleteBypassHelper.HasSoftDelete(entity))
            SoftDeleteBypassHelper.EmitBaseSqlVariable(sb, entity, prefix, varName);
    }

    private static string SelectSqlRef(EntityModel entity, string prefix, string baseVarName = "baseSql")
        => SoftDeleteBypassHelper.HasSoftDelete(entity) ? baseVarName : $"{prefix}Sql";

    private static void EmitGetByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, {SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"GetByIdAsync\";");
        EmitSoftDeleteBase(sb, entity, "SelectById");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectById")});");
        EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn,
            "sql", ReadParams(entity, "id"), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "SelectAll");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectAll")});");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            "sql", ReadParams(entity, ""), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "SelectAll");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectAll")}) + GetSortFragment(sort);");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            "sql", ReadParams(entity, ""), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "CountPage", "countBase");
        sb.AppendLine($"        var countSql = ApplyGlobalFilters({SelectSqlRef(entity, "CountPage", "countBase")});");
        EmitSoftDeleteBase(sb, entity, "SelectAllPage", "pageBase");
        sb.AppendLine($"        var pageSql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectAllPage", "pageBase")});");
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countSql, {ReadParams(entity, "")}, transaction);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageSql", ReadParams(entity, "offset = pageable.Offset, pageSize = pageable.PageSize"), "transaction", "content");
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
        EmitSoftDeleteBase(sb, entity, "CountPage", "countBase");
        sb.AppendLine($"        var countSql = ApplyGlobalFilters({SelectSqlRef(entity, "CountPage", "countBase")});");
        EmitSoftDeleteBase(sb, entity, "SelectAllPage", "pageBase");
        sb.AppendLine($"        var pageSql = ApplySortToPagedSql(ApplyGlobalFilters({SelectSqlRef(entity, "SelectAllPage", "pageBase")}), GetSortFragment(sort));");
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countSql, {ReadParams(entity, "")}, transaction);");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageSql", ReadParams(entity, "offset = pageable.Offset, pageSize = pageable.PageSize"), "transaction", "content");
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
        EmitSoftDeleteBase(sb, entity, "SelectAllSlice");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectAllSlice")});");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", ReadParams(entity, "offset = pageable.Offset, sliceSize = pageable.PageSize + 1"), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "SelectAllSlice");
        sb.AppendLine($"        var sql = ApplySortToPagedSql(ApplyGlobalFilters({SelectSqlRef(entity, "SelectAllSlice")}), GetSortFragment(sort));");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", ReadParams(entity, "offset = pageable.Offset, sliceSize = pageable.PageSize + 1"), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "SelectByIds");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "SelectByIds")});");
        EntityQueryEmitter.EmitQueryAsyncEnumerable(sb, entity, entityFqn,
            "sql", ReadParams(entity, "ids"), "transaction");
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
        EmitSoftDeleteBase(sb, entity, "Exists");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "Exists")});");
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, sql, {ReadParams(entity, "id")}, transaction) == 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitCountOverride(StringBuilder sb, EntityModel entity)
    {
        sb.AppendLine($"    public override async Task<long> CountAsync({SoftDeleteBypassHelper.IncludeDeletedParam}, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        const string MethodName = \"CountAsync\";");
        EmitSoftDeleteBase(sb, entity, "Count");
        sb.AppendLine($"        var sql = ApplyGlobalFilters({SelectSqlRef(entity, "Count")});");
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<long>(_connection, sql, {ReadParams(entity, "")}, transaction);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static bool TryGetChildModel(
        string childFqn,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel childModel)
    {
        childModel = null!;
        var key = childFqn.StartsWith("global::", StringComparison.Ordinal)
            ? childFqn.Substring("global::".Length)
            : childFqn;
        if (allModels.TryGetValue(key, out childModel!))
            return true;

        foreach (var model in allModels.Values)
        {
            if (model.FullyQualifiedName == childFqn
                || model.FullyQualifiedName == key
                || (model.FullyQualifiedName.StartsWith("global::", StringComparison.Ordinal)
                    && model.FullyQualifiedName.Substring("global::".Length) == key))
            {
                childModel = model;
                return true;
            }
        }

        return false;
    }
}
