using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Emitters;

using System.Text;
using Generator.Models;
using Generator.Utils;

internal static class EntityQueryEmitter
{
    public static void EmitQueryFirstOrDefaultAsync(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string sqlExpression,
        string paramExpression,
        string transactionExpression,
        string resultVariable = "result",
        bool emitLogContext = false)
    {
        var logSuffix = DbExecutorEmission.LogContextSuffixIf(emitLogContext);
        if (entity.RequiresDbRow)
        {
            var rowType = EmbeddedMappingEmitter.DbRowTypeName(entity);
            sb.AppendLine($"        var __row = await DbExecutor.QueryFirstOrDefaultAsync<{rowType}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix});");
            sb.AppendLine($"        var {resultVariable} = __row is null ? null : MapFromDbRow(__row);");
        }
        else
            sb.AppendLine($"        var {resultVariable} = await DbExecutor.QueryFirstOrDefaultAsync<{entityFqn}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix});");
    }

    public static void EmitQueryAsyncToList(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string sqlExpression,
        string paramExpression,
        string transactionExpression,
        string resultVariable = "results",
        bool emitLogContext = false)
    {
        var logSuffix = DbExecutorEmission.LogContextSuffixIf(emitLogContext);
        if (entity.RequiresDbRow)
        {
            var rowType = EmbeddedMappingEmitter.DbRowTypeName(entity);
            sb.AppendLine($"        var __rows = (await DbExecutor.QueryAsync<{rowType}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix})).AsList();");
            sb.AppendLine($"        var {resultVariable} = __rows.Select(MapFromDbRow).ToList();");
        }
        else
            sb.AppendLine($"        var {resultVariable} = (await DbExecutor.QueryAsync<{entityFqn}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix})).AsList();");
    }

    public static void EmitQueryAsyncEnumerable(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string sqlExpression,
        string paramExpression,
        string transactionExpression,
        string resultVariable = "results",
        bool emitLogContext = false)
    {
        var logSuffix = DbExecutorEmission.LogContextSuffixIf(emitLogContext);
        if (entity.RequiresDbRow)
        {
            var rowType = EmbeddedMappingEmitter.DbRowTypeName(entity);
            sb.AppendLine($"        var __rows = await DbExecutor.QueryAsync<{rowType}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix});");
            sb.AppendLine($"        var {resultVariable} = __rows.Select(MapFromDbRow);");
        }
        else
            sb.AppendLine($"        var {resultVariable} = await DbExecutor.QueryAsync<{entityFqn}>(_connection, {sqlExpression}, {paramExpression}, {transactionExpression}{logSuffix});");
    }

    public static string GetExecuteParameterExpression(EntityModel entity, string entityVariable = "entity")
        => entity.RequiresDbRow ? $"BuildMutationParameters({entityVariable})" : entityVariable;

    /// <summary>Overrides <c>DapperXRepositoryBase&lt;TEntity,TId&gt;</c> reads when DbRow mapping is required and tenancy/filters do not already override.</summary>
    public static void EmitStandardReadOverrides(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        EmitGetByIdOverride(sb, entity, entityFqn, idType);
        EmitGetAllOverride(sb, entity, entityFqn);
        EmitGetAllSortOverride(sb, entity, entityFqn);
        EmitGetAllPageOverride(sb, entity, entityFqn);
        EmitGetAllSortPageOverride(sb, entity, entityFqn);
        EmitGetAllSliceOverride(sb, entity, entityFqn);
        EmitGetAllSliceSortOverride(sb, entity, entityFqn);
        EmitFindAllByIdOverride(sb, entity, entityFqn, idType);
    }

    private static void EmitGetByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<{entityFqn}?> GetByIdAsync({idType} id, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn, "SelectByIdSql", "new { Id = id }", "transaction");
        sb.AppendLine("        ApplyPostLoad(result);");
        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync(bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        EmitQueryAsyncEnumerable(sb, entity, entityFqn, "SelectAllSql", "null", "transaction");
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSortOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> GetAllAsync(Sort sort, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var sql = SelectAllSql + GetSortFragment(sort);");
        EmitQueryAsyncEnumerable(sb, entity, entityFqn, "sql", "null", "transaction");
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllPageOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, CountPageSql, null, transaction);");
        EmitQueryAsyncToList(sb, entity, entityFqn,
            "SelectAllPageSql", "new { offset = pageable.Offset, pageSize = pageable.PageSize }", "transaction", "content");
        sb.AppendLine("        ApplyPostLoad(content);");
        sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSortPageOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Page<{entityFqn}>> GetAllAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, CountPageSql, null, transaction);");
        sb.AppendLine("        var pageSql = SelectAllPageSql + GetSortFragment(sort);");
        EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageSql", "new { offset = pageable.Offset, pageSize = pageable.PageSize }", "transaction", "content");
        sb.AppendLine("        ApplyPostLoad(content);");
        sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSliceOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        EmitQueryAsyncToList(sb, entity, entityFqn,
            "SelectAllSliceSql", "new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }", "transaction");
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine($"        return new Slice<{entityFqn}>(results, pageable.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitGetAllSliceSortOverride(StringBuilder sb, EntityModel entity, string entityFqn)
    {
        sb.AppendLine($"    public override async Task<Slice<{entityFqn}>> GetAllSliceAsync(Sort sort, Pageable pageable, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var sql = SelectAllSliceSql + GetSortFragment(sort);");
        EmitQueryAsyncToList(sb, entity, entityFqn,
            "sql", "new { offset = pageable.Offset, sliceSize = pageable.PageSize + 1 }", "transaction");
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine($"        return new Slice<{entityFqn}>(results, pageable.PageSize);");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void EmitFindAllByIdOverride(StringBuilder sb, EntityModel entity, string entityFqn, string idType)
    {
        sb.AppendLine($"    public override async Task<IEnumerable<{entityFqn}>> FindAllByIdAsync(IEnumerable<{idType}> ids, bool includeDeleted = false, IDbTransaction? transaction = null, CancellationToken ct = default)");
        sb.AppendLine("    {");
        EmitQueryAsyncEnumerable(sb, entity, entityFqn, "SelectByIdsSql", "new { ids }", "transaction");
        sb.AppendLine("        ApplyPostLoad(results);");
        sb.AppendLine("        return results;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }
}
