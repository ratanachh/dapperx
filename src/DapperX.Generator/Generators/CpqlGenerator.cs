namespace DapperX.Generator.Generators;

using System.Text;
using DapperX.Generator.Cpql;
using DapperX.Generator.Emitters;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using DapperX.Generator.Validation;
using Microsoft.CodeAnalysis;

internal static class CpqlGenerator
{
    public static string? TryEmitMethod(
        IMethodSymbol method,
        EntityModel entity,
        string cpql,
        string providerName,
        IReadOnlyDictionary<string, EntityModel> allModels,
        Compilation compilation,
        SourceProductionContext ctx)
    {
        var location = method.Locations.FirstOrDefault();
        CpqlStatementNode ast;
        try
        {
            ast = CpqlParser.Parse(cpql);
        }
        catch (CpqlParseException ex)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("DPXCPQL001", "CPQL parse error", ex.Message, "DapperX.CPQL",
                    DiagnosticSeverity.Error, true), location));
            return null;
        }

        if (!CpqlValidator.Validate(ast, entity, method, allModels, compilation, ctx, location))
            return null;

        DerivedQueryValidator.ValidateIncludeDeletedParameter(method, entity, ctx);

        var hasIncludeDeleted = method.Parameters.Any(p => p.Name == "includeDeleted");
        var usePairedSql = hasIncludeDeleted && entity.SoftDeleteColumn is not null;

        string sql;
        string? sqlIncludingDeleted = null;
        string? countSql = null;
        string? countSqlIncludingDeleted = null;
        try
        {
            sql = CpqlTranslator.Translate(ast, new CpqlTranslationContext(entity, providerName, allModels, applySoftDeleteFilter: true));
            if (usePairedSql)
                sqlIncludingDeleted = CpqlTranslator.Translate(ast, new CpqlTranslationContext(entity, providerName, allModels, applySoftDeleteFilter: false));
            if (method.ReturnType.ToDisplayString().Contains("Page<", StringComparison.Ordinal))
            {
                countSql = CpqlTranslator.TranslateCount(ast, new CpqlTranslationContext(entity, providerName, allModels, applySoftDeleteFilter: true));
                if (usePairedSql)
                    countSqlIncludingDeleted = CpqlTranslator.TranslateCount(ast, new CpqlTranslationContext(entity, providerName, allModels, applySoftDeleteFilter: false));
            }
        }
        catch (Exception ex)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("DPXCPQL020", "CPQL translation error", ex.Message, "DapperX.CPQL",
                    DiagnosticSeverity.Error, true), location));
            return null;
        }

        var translationCtx = new CpqlTranslationContext(entity, providerName, allModels);
        return EmitMethodBody(method, entity, sql, translationCtx, providerName, countSql, usePairedSql, sqlIncludingDeleted, countSqlIncludingDeleted);
    }

    private static string EmitMethodBody(
        IMethodSymbol method,
        EntityModel entity,
        string sql,
        CpqlTranslationContext translationCtx,
        string providerName,
        string? countSql = null,
        bool usePairedSql = false,
        string? sqlIncludingDeleted = null,
        string? countSqlIncludingDeleted = null)
    {
        var entityFqn = entity.FullyQualifiedName;
        var sig = BuildSignature(method, entityFqn);
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"{method.Name}\";");
        if (usePairedSql && sqlIncludingDeleted is not null)
        {
            sb.AppendLine($"        const string sqlActiveOnly = \"{Esc(sql)}\";");
            sb.AppendLine($"        const string sqlIncludingDeleted = \"{Esc(sqlIncludingDeleted)}\";");
            sb.AppendLine("        var sql = includeDeleted ? sqlIncludingDeleted : sqlActiveOnly;");
            GlobalFilterGenerator.EmitCpqlApplyGlobalFilters(sb, entity);
        }
        else
        {
            sb.AppendLine($"        const string sql = \"{Esc(sql)}\";");
            GlobalFilterGenerator.EmitCpqlApplyGlobalFilters(sb, entity);
        }

        var paramList = BuildCpqlParams(method, translationCtx, entity);
        var paramArg = string.IsNullOrEmpty(paramList) ? "null" : $"new {{ {paramList} }}";
        var tx = TransactionExpression(method);
        var returnType = method.ReturnType.ToDisplayString();

        if (returnType.Contains("IAsyncEnumerable"))
        {
            var inner = GetTaskInnerType(method) ?? entityFqn;
            sb.AppendLine($"        return DbExecutor.QueryUnbufferedAsync<{inner}>(_connection, sql, {paramArg}, {tx}{DbExecutorEmission.LogContextSuffix});");
        }
        else if (returnType.Contains("Slice<"))
        {
            EmitSliceBody(sb, method, entity, entityFqn, paramList, tx, providerName);
        }
        else if (returnType.Contains("Page<"))
        {
            EmitPageBody(sb, method, entity, entityFqn, paramList, tx, providerName, countSql ?? sql, usePairedSql, countSqlIncludingDeleted);
        }
        else if (returnType.Contains("Task<int>") || returnType.Contains("Task<long>"))
        {
            sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<long>(_connection, sql, {paramArg}, {tx}{DbExecutorEmission.LogContextSuffix});");
        }
        else if (returnType.Contains("Task<bool>"))
        {
            sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, sql, {paramArg}, {tx}{DbExecutorEmission.LogContextSuffix}) == 1;");
        }
        else if (returnType.Contains("Task") && returnType.Contains("Execute"))
        {
            sb.AppendLine($"        return await DbExecutor.ExecuteAsync(_connection, sql, {paramArg}, {tx}, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
        }
        else if (IsSingleEntityReturn(method))
        {
            var inner = GetTaskInnerType(method) ?? entityFqn;
            if (entity.RequiresDbRow && inner == entityFqn)
            {
                EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn, "sql", paramArg, tx, "__entity", emitLogContext: true);
            }
            else
                sb.AppendLine($"        var __entity = await DbExecutor.QueryFirstOrDefaultAsync<{inner}>(_connection, sql, {paramArg}, {tx}{DbExecutorEmission.LogContextSuffix});");
            if (entity.HasPostLoad)
                sb.AppendLine("        if (__entity is not null) OnPostLoad(__entity);");
            sb.AppendLine("        return __entity;");
        }
        else
        {
            var inner = GetEnumerableInnerType(method) ?? entityFqn;
            if (entity.RequiresDbRow && inner == entityFqn)
            {
                EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn, "sql", paramArg, tx, "__entities", emitLogContext: true);
            }
            else
                sb.AppendLine($"        var __entities = (await DbExecutor.QueryAsync<{inner}>(_connection, sql, {paramArg}, {tx}{DbExecutorEmission.LogContextSuffix})).AsList();");
            if (entity.HasPostLoad)
                sb.AppendLine("        foreach (var __e in __entities) OnPostLoad(__e);");
            sb.AppendLine("        return __entities;");
        }

        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static void EmitSliceBody(StringBuilder sb, IMethodSymbol method, EntityModel entity, string entityFqn,
        string paramList, string tx, string provider)
    {
        var template = provider == "SqlServer"
            ? " OFFSET @offset ROWS FETCH NEXT @sliceSize ROWS ONLY"
            : " LIMIT @sliceSize OFFSET @offset";
        sb.AppendLine($"        var sliceSql = sql + \"{template}\";");
        sb.AppendLine("        var sliceSize = pageable.PageSize + 1;");
        var p = string.IsNullOrEmpty(paramList) ? "" : paramList + ", ";
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "sliceSql", $"new {{ {p}offset = pageable.Offset, sliceSize }}", tx, "rawResults", emitLogContext: true);
        if (entity.HasPostLoad)
            sb.AppendLine("        foreach (var __e in rawResults) OnPostLoad(__e);");
        sb.AppendLine($"        return new Slice<{entityFqn}>(rawResults, pageable.PageSize);");
    }

    private static void EmitPageBody(StringBuilder sb, IMethodSymbol method, EntityModel entity, string entityFqn,
        string paramList, string tx, string provider, string countSql, bool usePairedSql = false, string? countSqlIncludingDeleted = null)
    {
        var paging = provider == "SqlServer"
            ? " OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY"
            : " LIMIT @pageSize OFFSET @offset";
        if (usePairedSql && countSqlIncludingDeleted is not null)
        {
            sb.AppendLine($"        const string countSqlActiveOnly = \"{Esc(countSql)}\";");
            sb.AppendLine($"        const string countSqlIncludingDeleted = \"{Esc(countSqlIncludingDeleted)}\";");
            sb.AppendLine("        var countSql = includeDeleted ? countSqlIncludingDeleted : countSqlActiveOnly;");
        }
        else
            sb.AppendLine($"        const string countSql = \"{Esc(countSql)}\";");
        GlobalFilterGenerator.EmitCpqlApplyGlobalFiltersCount(sb, entity);
        sb.AppendLine($"        var pageSql = sql + \"{paging}\";");
        var p = string.IsNullOrEmpty(paramList) ? "" : paramList + ", ";
        sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countSql, new {{ {paramList} }}, {tx}{DbExecutorEmission.LogContextSuffix});");
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            "pageSql", $"new {{ {p}offset = pageable.Offset, pageSize = pageable.PageSize }}", tx, "content", emitLogContext: true);
        if (entity.HasPostLoad)
            sb.AppendLine("        foreach (var __e in content) OnPostLoad(__e);");
        sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
    }

    private static string BuildCpqlParams(IMethodSymbol method, CpqlTranslationContext ctx, EntityModel entity)
    {
        var parts = new List<string>();
        foreach (var name in ctx.Parameters.OrderBy(p => p))
        {
            var sym = method.Parameters.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (sym is not null)
                parts.Add($"{sym.Name} = {sym.Name}");
        }
        if (entity.TenantIdColumn is not null)
            parts.Add("tenantId = _tenantProvider?.GetCurrentTenantId()");
        if (method.Parameters.Any(p => p.Name == "transaction"))
            parts.Add("transaction = transaction");
        if (method.Parameters.Any(p => p.Name == "ct"))
            parts.Add("ct = ct");
        if (method.Parameters.Any(p => p.Name == "pageable"))
        {
            parts.Add("offset = pageable.Offset");
            parts.Add("pageSize = pageable.PageSize");
        }
        return string.Join(", ", parts);
    }

    private static bool IsSingleEntityReturn(IMethodSymbol method)
    {
        var inner = GetTaskInnerType(method);
        if (inner is null) return false;
        return !inner.Contains("IEnumerable") && !inner.Contains("List") && !inner.Contains("Collection");
    }

    private static string? GetTaskInnerType(IMethodSymbol method)
    {
        if (method.ReturnType is not INamedTypeSymbol task || !task.IsGenericType)
            return null;
        return task.TypeArguments[0].ToDisplayString();
    }

    private static string? GetEnumerableInnerType(IMethodSymbol method)
    {
        var inner = GetTaskInnerType(method);
        if (inner is null) return null;
        if (inner.Contains("IEnumerable<") && inner.EndsWith(">"))
        {
            var start = inner.IndexOf('<') + 1;
            return inner.Substring(start, inner.Length - start - 1);
        }
        return inner;
    }

    private static string TransactionExpression(IMethodSymbol method)
        => method.Parameters.Any(p => p.Name == "transaction") ? "transaction" : "null";

    private static string BuildSignature(IMethodSymbol method, string entityFqn)
    {
        var ret = method.ReturnType.ToDisplayString();
        var parms = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));
        return $"public async {ret} {method.Name}({parms})";
    }

    private static string Esc(string s) => s.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
