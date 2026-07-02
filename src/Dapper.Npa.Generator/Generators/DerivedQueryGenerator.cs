using Dapper.Npa.Generator.Builders;
using Dapper.Npa.Generator.Emitters;
using Dapper.Npa.Generator.MethodNameParsing;
using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;
using Dapper.Npa.Generator.Validation;

namespace Dapper.Npa.Generator.Generators;

using System.Text;
using Generator.Builders;
using Generator.Emitters;
using Generator.MethodNameParsing;
using Generator.Models;
using Generator.Utils;
using Generator.Validation;
using Microsoft.CodeAnalysis;

/// <summary>
/// Generates repository method implementations for derived query methods declared on repository interfaces.
/// All SQL is emitted as compile-time string literals.
/// Property→column resolution uses the generated ResolveColumn() switch.
/// </summary>
internal static class DerivedQueryGenerator
{
    public static string? TryEmitMethod(
        IMethodSymbol method,
        EntityModel entity,
        string provider,
        Compilation compilation,
        SourceProductionContext ctx,
        IReadOnlyDictionary<string, EntityModel>? allModels = null)
    {
        var methodName = method.Name;
        var pathKeys = entity.DerivedQueryPaths.Select(p => p.PathKey).ToList();

        DerivedQueryValidator.ValidateIncludeDeletedParameter(method, entity, ctx);
        ValidateSqliteRepositoryMethod(method, entity, provider, ctx);

        var bulkAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.BulkOperationAttr);

        // Custom query attributes take priority over name derivation
        var queryAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.QueryAttr);
        if (queryAttr is not null)
            return EmitQueryAttributeMethod(method, entity, queryAttr, provider, compilation, ctx, allModels);

        var namedQueryAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.NamedQueryAttr);
        if (namedQueryAttr is not null)
            return EmitNamedQueryMethod(method, entity, namedQueryAttr, provider, compilation, ctx, allModels);

        var spAttr = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.StoredProcedureAttr);
        if (spAttr is not null)
        {
            if (provider == "Sqlite")
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.StoredProcedureNotSupportedOnSqlite,
                    method.Locations.FirstOrDefault()));
                return null;
            }
            if (!StoredProcedureValidator.Validate(method, entity, spAttr, provider, ctx))
                return null;
            return StoredProcedureGenerator.EmitMethod(method, entity, spAttr, provider);
        }

        // Method name derivation
        var parseResult = MethodNameParser.TryParseDetailed(methodName, pathKeys);
        if (parseResult.IsAmbiguous)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.AmbiguousDerivedQueryPath,
                method.Locations.FirstOrDefault(),
                methodName, entity.ClassName, methodName));
            return null;
        }

        var parsed = parseResult.Query;
        if (parsed is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.DerivedMethodNotParsed,
                method.Locations.FirstOrDefault(),
                methodName, entity.ClassName));
            return null;
        }

        if (bulkAttr is not null)
        {
            if (!DerivedQueryValidator.ValidateBulkOperation(method, entity, parsed, ctx))
                return null;
            return EmitBulkOperationMethod(method, entity, parsed);
        }

        if (!DerivedQueryValidator.ValidateParsedQuery(method, entity, parsed, provider, ctx))
            return null;

        if (!DerivedQueryValidator.ValidateEntityGraphUsage(method, entity, parsed, ctx))
            return null;

        return EmitDerivedMethod(method, entity, parsed, provider, ctx);
    }

    // ─── Derived method emission ───────────────────────────────────────────────

    private static string EmitDerivedMethod(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        string provider,
        SourceProductionContext ctx)
    {
        var sb = new StringBuilder();
        var entityFqn = entity.FullyQualifiedName;
        var returnType = method.ReturnType.ToDisplayString();
        var sig = BuildSignature(method, entityFqn);

        bool hasSort = method.Parameters.Any(p => p.Type.Name == "Sort");
        bool hasEntityGraph = DerivedQueryValidator.HasEntityGraphParameter(method)
            && entity.NamedEntityGraphs.Count > 0;

        if (parsed.Subject is SubjectKind.Insert or SubjectKind.Update)
        {
            sb.AppendLine($"    {sig}");
            sb.AppendLine("    {");
            if (parsed.Subject == SubjectKind.Insert)
                EmitInsertBody(sb, method, entity);
            else
                EmitUpdateBody(sb, method, entity);
            sb.AppendLine("    }");
            return sb.ToString();
        }

        var usedPaths = CollectUsedPaths(entity, parsed, includeSortablePaths: hasSort);
        var hasJoin = usedPaths.Any(p => p.Kind == DerivedQueryPathKind.NavigationJoin);
        var selectCore = hasJoin
            ? DerivedQueryPathBuilder.BuildSelectWithJoins(entity, usedPaths)
            : SqlBuilder.BuildSelectCore(entity);
        var whereSql = BuildWhereSql(parsed.Conditions, entity, provider, hasJoin);
        var orderSql = BuildOrderSql(parsed.OrderBySegments, entity, hasJoin);

        bool hasPageable = method.Parameters.Any(p => p.Type.Name == "Pageable");
        bool hasIncludeDeleted = method.Parameters.Any(p => p.Name == "includeDeleted");
        bool hasLockMode = method.Parameters.Any(p => p.Type.Name == "LockMode");
        bool hasTransaction = method.Parameters.Any(p => p.Name == "transaction");
        bool isSlice = returnType.Contains("Slice<");
        bool isPage = returnType.Contains("Page<");
        bool isAsync = returnType.Contains("IAsyncEnumerable");

        if (hasLockMode && !hasTransaction)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.PessimisticLockWithoutTransactionContext,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
        }

        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string MethodName = \"{method.Name}\";");

        switch (parsed.Subject)
        {
            case SubjectKind.Find:
            case SubjectKind.Stream:
                EmitSelectBody(sb, method, entity, entityFqn, whereSql, orderSql, selectCore,
                    parsed, hasSort, hasPageable, hasIncludeDeleted, hasLockMode, isSlice, isPage, isAsync, provider, hasJoin, hasEntityGraph);
                break;

            case SubjectKind.Count:
            case SubjectKind.CountDistinct:
                EmitCountBody(sb, method, entity, whereSql, parsed, hasIncludeDeleted, provider, hasJoin);
                break;

            case SubjectKind.Exists:
                EmitExistsBody(sb, method, entity, whereSql, parsed, hasIncludeDeleted, provider, hasJoin);
                break;

            case SubjectKind.Delete:
                EmitDeleteBody(sb, method, entity, whereSql, parsed, provider, hasJoin);
                break;

            case SubjectKind.Insert:
            case SubjectKind.Update:
                break;

            default:
                sb.AppendLine("        throw new NotImplementedException();");
                break;
        }

        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static void EmitSelectBody(
        StringBuilder sb, IMethodSymbol method, EntityModel entity, string entityFqn,
        string whereSql, string orderSql, string selectCore,
        ParsedDerivedQuery parsed, bool hasSort, bool hasPageable,
        bool hasIncludeDeleted, bool hasLockMode, bool isSlice, bool isPage, bool isAsync,
        string provider, bool hasJoin, bool hasEntityGraph)
    {
        EmitDerivedSelectSqlVariables(sb, entity, selectCore, whereSql, orderSql, parsed, hasIncludeDeleted, provider, hasSort, hasJoin, deferSqlAssignment: hasEntityGraph);

        if (hasEntityGraph)
        {
            EmitEntityGraphSqlSelection(sb, entity, whereSql, hasIncludeDeleted, provider, hasSort);
            if (!hasSort)
                EmitApplyGlobalFiltersIfNeeded(sb, entity);
        }

        if (hasSort)
        {
            EmitDerivedSortSwitch(sb, entity, entityFqn, hasJoin);
            sb.AppendLine(hasEntityGraph
                ? "        var sql = entityGraph is null ? baseSql + orderBy : graphSql + orderBy;"
                : "        var sql = baseSql + orderBy;");
        }

        if (!hasEntityGraph || hasSort)
            EmitApplyGlobalFiltersIfNeeded(sb, entity);

        if (hasLockMode)
            EmitLockSuffixAppend(sb, provider);

        if (hasPageable && isSlice)
        {
            var sliceTemplate = provider == "SqlServer"
                ? " OFFSET @offset ROWS FETCH NEXT @sliceSize ROWS ONLY"
                : " LIMIT @sliceSize OFFSET @offset";
            if (!hasSort) sb.AppendLine($"        const string sliceSql = sql + \"{sliceTemplate}\";");
            else sb.AppendLine($"        var sliceSql = sql + \"{sliceTemplate}\";");
            sb.AppendLine("        var sliceSize = pageable.PageSize + 1;");
            EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
                "sliceSql", $"new {{ {BuildParams(method, parsed, entity)} offset = pageable.Offset, sliceSize }}", TransactionExpression(method), "rawResults", emitLogContext: true);
            EmitPostLoadMany(sb, entity, "rawResults");
            sb.AppendLine($"        return new Slice<{entityFqn}>(rawResults, pageable.PageSize);");
        }
        else if (hasPageable && isPage)
        {
            var pageTemplate = provider == "SqlServer"
                ? " OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY"
                : " LIMIT @pageSize OFFSET @offset";
            EmitDerivedCountSqlVariable(sb, entity, whereSql, hasIncludeDeleted, provider, "countSql", parsed: parsed, hasJoin: hasJoin);
            EmitApplyGlobalFiltersIfNeeded(sb, entity, "countSql");
            if (!hasSort) sb.AppendLine($"        var pageSql = sql + \"{pageTemplate}\";");
            else sb.AppendLine($"        var pageSql = sql + \"{pageTemplate}\";");
            sb.AppendLine($"        var total = await DbExecutor.ExecuteScalarAsync<long>(_connection, countSql, new {{ {BuildParams(method, parsed, entity)} }}, {TransactionExpression(method)}{DbExecutorEmission.LogContextSuffix});");
            EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
                "pageSql", $"new {{ {BuildParams(method, parsed, entity)} offset = pageable.Offset, pageSize = pageable.PageSize }}", TransactionExpression(method), "content", emitLogContext: true);
            EmitPostLoadMany(sb, entity, "content");
            sb.AppendLine($"        return new Page<{entityFqn}>(content, total, pageable);");
        }
        else if (isAsync)
        {
            sb.AppendLine($"        return DbExecutor.QueryUnbufferedAsync<{entityFqn}>(_connection, sql, new {{ {BuildParams(method, parsed, entity)} }}, {TransactionExpression(method)}{DbExecutorEmission.LogContextSuffix});");
        }
        else if (parsed.LimitN == 1 || IsSingleEntityReturn(method))
        {
            EntityQueryEmitter.EmitQueryFirstOrDefaultAsync(sb, entity, entityFqn,
                "sql", $"new {{ {BuildParams(method, parsed, entity)} }}", TransactionExpression(method), "__entity", emitLogContext: true);
            EmitPostLoadSingle(sb, entity, "__entity");
            sb.AppendLine("        return __entity;");
        }
        else
        {
            EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
                "sql", $"new {{ {BuildParams(method, parsed, entity)} }}", TransactionExpression(method), "__entities", emitLogContext: true);
            EmitPostLoadMany(sb, entity, "__entities");
            sb.AppendLine("        return __entities;");
        }
    }

    private static bool IsSingleEntityReturn(IMethodSymbol method)
    {
        if (method.ReturnType is not INamedTypeSymbol taskType
            || !taskType.IsGenericType
            || taskType.TypeArguments.Length != 1)
            return false;

        var inner = taskType.TypeArguments[0];
        if (inner.TypeKind == TypeKind.Array)
            return false;

        var innerDisplay = inner.ToDisplayString();
        if (innerDisplay.Contains("IEnumerable")
            || innerDisplay.Contains("IReadOnlyList<")
            || innerDisplay.Contains("IReadOnlyCollection<")
            || innerDisplay.Contains("IList<")
            || innerDisplay.Contains("ICollection<")
            || innerDisplay.Contains("List<")
            || innerDisplay.Contains("Page<")
            || innerDisplay.Contains("Slice<"))
            return false;

        return true;
    }

    private static void EmitPostLoadSingle(StringBuilder sb, EntityModel entity, string variableName)
    {
        if (!entity.HasPostLoad)
            return;
        sb.AppendLine($"        if ({variableName} is not null) OnPostLoad({variableName});");
    }

    private static void EmitPostLoadMany(StringBuilder sb, EntityModel entity, string collectionName)
    {
        if (!entity.HasPostLoad)
            return;
        sb.AppendLine($"        foreach (var __e in {collectionName}) OnPostLoad(__e);");
    }

    private static void EmitCountBody(
        StringBuilder sb, IMethodSymbol method, EntityModel entity, string whereSql,
        ParsedDerivedQuery parsed, bool hasIncludeDeleted, string provider, bool hasJoin)
    {
        var idCol = entity.Properties.First(p => p.IsId).ColumnName;
        var countCol = parsed.Subject == SubjectKind.CountDistinct
            ? $"DISTINCT {(hasJoin ? $"e.{idCol}" : idCol)}"
            : "*";
        var countCore = hasJoin
            ? $"SELECT COUNT({countCol}) FROM {BuildFromWithJoins(entity, CollectUsedPaths(entity, parsed))}"
            : $"SELECT COUNT({countCol}) FROM {entity.TableName}";
        EmitDerivedCountSqlVariable(sb, entity, whereSql, hasIncludeDeleted, provider, "sql", countCore, parsed, hasJoin);
        EmitApplyGlobalFiltersIfNeeded(sb, entity);
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<long>(_connection, sql, new {{ {BuildParams(method, parsed, entity)} }}, {TransactionExpression(method)}{DbExecutorEmission.LogContextSuffix});");
    }

    private static void EmitExistsBody(
        StringBuilder sb, IMethodSymbol method, EntityModel entity, string whereSql,
        ParsedDerivedQuery parsed, bool hasIncludeDeleted, string provider, bool hasJoin)
    {
        EmitDerivedExistsSqlVariable(sb, entity, whereSql, hasIncludeDeleted, provider, "sql", parsed, hasJoin);
        EmitApplyGlobalFiltersIfNeeded(sb, entity);
        var paramList = BuildParams(method, parsed, entity);
        var paramArg = string.IsNullOrEmpty(paramList) ? "null" : $"new {{ {paramList.TrimEnd(',', ' ')} }}";
        sb.AppendLine($"        return await DbExecutor.ExecuteScalarAsync<int>(_connection, sql, {paramArg}, {TransactionExpression(method)}{DbExecutorEmission.LogContextSuffix}) == 1;");
    }

    private static void EmitInsertBody(StringBuilder sb, IMethodSymbol method, EntityModel entity)
    {
        var entityParam = DerivedQueryValidator.FindEntityParameter(method, entity)!.Name;
        var args = BuildBaseMethodArgs(entityParam, method);
        sb.AppendLine($"        await InsertAsync({args});");
    }

    private static void EmitUpdateBody(StringBuilder sb, IMethodSymbol method, EntityModel entity)
    {
        var entityParam = DerivedQueryValidator.FindEntityParameter(method, entity)!.Name;
        var args = BuildBaseMethodArgs(entityParam, method);
        sb.AppendLine($"        await UpdateAsync({args});");
    }

    private static string BuildBaseMethodArgs(string entityParam, IMethodSymbol method)
    {
        var parts = new List<string> { entityParam };
        if (method.Parameters.Any(p => p.Name == "transaction"))
            parts.Add("transaction");
        if (method.Parameters.Any(p => p.Name == "ct"))
            parts.Add("ct");
        return string.Join(", ", parts);
    }

    private static string EmitBulkOperationMethod(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed)
    {
        var collectionParam = DerivedQueryValidator.FindEntityCollectionParameter(method, entity)!.Name;
        var args = BuildBulkMethodArgs(collectionParam, method);
        var baseMethod = parsed.Subject switch
        {
            SubjectKind.Insert => "InsertManyAsync",
            SubjectKind.Update => "UpdateManyAsync",
            SubjectKind.Delete => "DeleteManyAsync",
            _ => null,
        };

        if (baseMethod is null)
            return string.Empty;

        var entityFqn = entity.FullyQualifiedName;
        var sig = BuildSignature(method, entityFqn);
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        await {baseMethod}({args});");
        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static string BuildBulkMethodArgs(string collectionParam, IMethodSymbol method)
    {
        var parts = new List<string> { collectionParam };
        if (method.Parameters.Any(p => p.Name == "transaction"))
            parts.Add("transaction");
        if (method.Parameters.Any(p => p.Name == "ct"))
            parts.Add("ct");
        return string.Join(", ", parts);
    }

    private static void EmitDeleteBody(
        StringBuilder sb, IMethodSymbol method, EntityModel entity, string whereSql, ParsedDerivedQuery parsed,
        string provider, bool hasJoin)
    {
        var usedPaths = CollectUsedPaths(entity, parsed);
        var where = BuildWhereClause(entity, whereSql, applySoftDelete: true, provider, hasJoin);
        string sql;
        if (hasJoin)
        {
            var from = BuildFromWithJoins(entity, usedPaths);
            if (entity.SoftDeleteColumn is not null)
            {
                var setClause = SoftDeleteGenerator.BuildSoftDeleteSetClause(entity, provider, "e.");
                sql = string.IsNullOrEmpty(where)
                    ? $"UPDATE e SET {setClause} FROM {from}"
                    : $"UPDATE e SET {setClause} FROM {from} WHERE {where}";
            }
            else
            {
                sql = string.IsNullOrEmpty(where)
                    ? $"DELETE e FROM {from}"
                    : $"DELETE e FROM {from} WHERE {where}";
            }
        }
        else
        {
            sql = entity.SoftDeleteColumn is not null
                ? SoftDeleteGenerator.BuildSoftDeleteUpdate(entity, provider, $"WHERE {where}")
                : $"DELETE FROM {entity.TableName} WHERE {where}";
        }

        sb.AppendLine($"        const string sql = \"{Esc(sql)}\";");
        var paramList = BuildParams(method, parsed, entity);
        var paramArg = string.IsNullOrEmpty(paramList) ? "null" : $"new {{ {paramList.TrimEnd(',', ' ')} }}";
        sb.AppendLine($"        return await DbExecutor.ExecuteAsync(_connection, sql, {paramArg}, {TransactionExpression(method)}, logContext: DbExecutor.CreateLogContext(MethodName, Options, Provider));");
    }

    // ─── Custom attribute methods ──────────────────────────────────────────────

    private static string? EmitQueryAttributeMethod(
        IMethodSymbol method,
        EntityModel entity,
        AttributeData attr,
        string provider,
        Compilation compilation,
        SourceProductionContext ctx,
        IReadOnlyDictionary<string, EntityModel>? allModels)
    {
        var query = SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty;
        var isNative = SyntaxHelper.GetNamedArg<bool?>(attr, "NativeQuery") == true;

        if (!isNative)
        {
            if (allModels is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CpqlTranslationNotImplemented,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    entity.ClassName));
                return null;
            }
            return CpqlGenerator.TryEmitMethod(method, entity, query, provider, allModels, compilation, ctx);
        }

        var entityFqn = entity.FullyQualifiedName;
        var sig = BuildSignature(method, entityFqn);
        var sb = new StringBuilder();
        sb.AppendLine($"    {sig}");
        sb.AppendLine("    {");
        sb.AppendLine($"        const string sql = \"{Esc(query)}\";");
        EmitQueryAsyncReturnWithPostLoad(sb, entity, entityFqn, "sql", TransactionExpression(method));
        sb.AppendLine("    }");
        return sb.ToString();
    }

    private static string? EmitNamedQueryMethod(
        IMethodSymbol method,
        EntityModel entity,
        AttributeData attr,
        string provider,
        Compilation compilation,
        SourceProductionContext ctx,
        IReadOnlyDictionary<string, EntityModel>? allModels)
    {
        var queryName = SyntaxHelper.GetConstructorArg<string>(attr, 0) ?? string.Empty;
        var named = entity.NamedQueries.FirstOrDefault(q => q.Name == queryName);
        var sqlText = named?.Query ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sqlText))
        {
            var entityFqn = entity.FullyQualifiedName;
            var sig = BuildSignature(method, entityFqn);
            var sb = new StringBuilder();
            sb.AppendLine($"    {sig}");
            sb.AppendLine("    {");
            sb.AppendLine($"        const string sql = \"/* named query '{queryName}' not found */\";");
            EmitQueryAsyncReturnWithPostLoad(sb, entity, entityFqn, "sql", TransactionExpression(method));
            sb.AppendLine("    }");
            return sb.ToString();
        }

        var trimmed = sqlText.TrimStart();
        if (allModels is not null
            && (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)))
        {
            var cpql = CpqlGenerator.TryEmitMethod(method, entity, sqlText, provider, allModels, compilation, ctx);
            if (cpql is not null)
                return cpql;
        }

        var fqn = entity.FullyQualifiedName;
        var signature = BuildSignature(method, fqn);
        var fallback = new StringBuilder();
        fallback.AppendLine($"    {signature}");
        fallback.AppendLine("    {");
        fallback.AppendLine($"        const string sql = \"{Esc(sqlText)}\";");
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            EmitQueryAsyncReturnWithPostLoad(fallback, entity, fqn, "sql", TransactionExpression(method));
        else
            fallback.AppendLine($"        return await DbExecutor.QueryAsync<{fqn}>(_connection, sql, null, {TransactionExpression(method)});");
        fallback.AppendLine("    }");
        return fallback.ToString();
    }

    private static void EmitQueryAsyncReturnWithPostLoad(
        StringBuilder sb,
        EntityModel entity,
        string entityFqn,
        string sqlExpression,
        string transactionExpression,
        string parametersExpression = "null")
    {
        EntityQueryEmitter.EmitQueryAsyncToList(sb, entity, entityFqn,
            sqlExpression, parametersExpression, transactionExpression, "__entities");
        EmitPostLoadMany(sb, entity, "__entities");
        sb.AppendLine("        return __entities;");
    }

    private static void ValidateSqliteRepositoryMethod(
        IMethodSymbol method,
        EntityModel entity,
        string provider,
        SourceProductionContext ctx)
    {
        if (provider != "Sqlite") return;
        var loc = method.Locations.FirstOrDefault();

        if (method.Parameters.Any(p => p.Type.Name == "LockMode"))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.LockModeNotSupportedOnSqlite,
                loc, method.Name, entity.ClassName));
        }

        var ret = method.ReturnType.ToDisplayString();
        if (ret.Contains("MultiResult", StringComparison.Ordinal))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MultipleResultSetsNotSupportedOnSqlite,
                loc, method.Name, entity.ClassName));
        }
    }

    // ─── SQL helpers ──────────────────────────────────────────────────────────

    private static IReadOnlyList<DerivedQueryPathModel> CollectUsedPaths(
        EntityModel entity,
        ParsedDerivedQuery parsed,
        bool includeSortablePaths = false)
    {
        var keys = parsed.Conditions.Select(c => c.PropertyName)
            .Concat(parsed.OrderBySegments.Select(s => s.PropertyName))
            .ToList();

        if (includeSortablePaths)
        {
            keys.AddRange(entity.DerivedQueryPaths
                .Where(p => p.IsSortable)
                .Select(p => p.PathKey));
        }

        return keys
            .Distinct(StringComparer.Ordinal)
            .Select(k => entity.DerivedQueryPaths.FirstOrDefault(p => p.PathKey == k))
            .Where(p => p is not null)
            .Cast<DerivedQueryPathModel>()
            .ToList();
    }

    private static string ResolveSelectCore(EntityModel entity, ParsedDerivedQuery parsed, bool hasSort)
    {
        var used = CollectUsedPaths(entity, parsed, includeSortablePaths: hasSort);
        if (used.Any(p => p.Kind == DerivedQueryPathKind.NavigationJoin))
            return DerivedQueryPathBuilder.BuildSelectWithJoins(entity, used);
        return SqlBuilder.BuildSelectCore(entity);
    }

    private static string BuildFromWithJoins(EntityModel entity, IEnumerable<DerivedQueryPathModel> usedPaths)
    {
        var joinPaths = usedPaths
            .Where(p => p.Kind == DerivedQueryPathKind.NavigationJoin)
            .GroupBy(p => p.JoinAlias)
            .Select(g => g.First())
            .ToList();

        var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
        var from = $"{table} e";
        foreach (var join in joinPaths)
            from += $" INNER JOIN {join.JoinTable} {join.JoinAlias} ON {join.JoinOnSql}";
        return from;
    }

    private static DerivedQueryPathModel? ResolvePath(EntityModel entity, string pathKey)
        => entity.DerivedQueryPaths.FirstOrDefault(p => p.PathKey == pathKey);

    private static string ToParamName(string pathKey)
        => pathKey.Length == 0
            ? pathKey
            : char.ToLowerInvariant(pathKey[0]) + pathKey.Substring(1);

    private static string BuildWhereSql(IReadOnlyList<ConditionGroup> conditions, EntityModel entity, string provider, bool hasJoin)
    {
        if (!conditions.Any()) return string.Empty;
        var parts = new List<string>();
        foreach (var c in conditions)
        {
            var path = ResolvePath(entity, c.PropertyName);
            var fallback = SyntaxHelper.ToSnakeCase(c.PropertyName);
            var col = DerivedQueryPathBuilder.QualifyColumn(path, fallback, hasJoin);
            var paramName = ToParamName(c.PropertyName);

            var predicate = c.Operator switch
            {
                OperatorKind.Equal            => $"{col} = @{paramName}",
                OperatorKind.NotEqual         => $"{col} <> @{paramName}",
                OperatorKind.GreaterThan      => $"{col} > @{paramName}",
                OperatorKind.GreaterThanEqual => $"{col} >= @{paramName}",
                OperatorKind.LessThan         => $"{col} < @{paramName}",
                OperatorKind.LessThanEqual    => $"{col} <= @{paramName}",
                OperatorKind.Between          => $"{col} BETWEEN @{paramName}Start AND @{paramName}End",
                OperatorKind.Like             => $"{col} LIKE @{paramName}",
                OperatorKind.NotLike          => $"{col} NOT LIKE @{paramName}",
                OperatorKind.Containing       => $"{col} LIKE '%' + @{paramName} + '%'",
                OperatorKind.NotContaining    => $"{col} NOT LIKE '%' + @{paramName} + '%'",
                OperatorKind.StartingWith     => $"{col} LIKE @{paramName} + '%'",
                OperatorKind.EndingWith       => $"{col} LIKE '%' + @{paramName}",
                OperatorKind.In               => ProviderSqlHelper.InClause(col, paramName, provider),
                OperatorKind.NotIn            => ProviderSqlHelper.NotInClause(col, paramName, provider),
                OperatorKind.IsNull           => $"{col} IS NULL",
                OperatorKind.IsNotNull        => $"{col} IS NOT NULL",
                OperatorKind.IsTrue           => $"{col} = {ProviderSqlHelper.BooleanLiteral(true, provider)}",
                OperatorKind.IsFalse          => $"{col} = {ProviderSqlHelper.BooleanLiteral(false, provider)}",
                OperatorKind.Before           => $"{col} < @{paramName}",
                OperatorKind.After            => $"{col} > @{paramName}",
                OperatorKind.IgnoreCase       => $"LOWER({col}) = LOWER(@{paramName})",
                OperatorKind.Regex            => BuildRegexPredicate(col, paramName, provider),
                _                             => $"{col} = @{paramName}",
            };

            if (parts.Any())
                parts.Add(c.Connector == LogicalConnector.Or ? " OR " : " AND ");
            parts.Add(predicate);
        }
        return string.Concat(parts);
    }

    private static string BuildRegexPredicate(string column, string paramName, string provider)
        => provider switch
        {
            "PostgreSql" => $"{column} ~ @{paramName}",
            "MySql"      => $"{column} REGEXP @{paramName}",
            "Sqlite"     => $"{column} REGEXP @{paramName}",
            _            => $"{column} REGEXP @{paramName}",
        };

    private static string BuildOrderSql(IReadOnlyList<OrderBySegment> segments, EntityModel entity, bool hasJoin)
    {
        if (!segments.Any()) return string.Empty;
        var parts = segments.Select(s =>
        {
            var path = ResolvePath(entity, s.PropertyName);
            var fallback = SyntaxHelper.ToSnakeCase(s.PropertyName);
            var col = DerivedQueryPathBuilder.QualifyColumn(path, fallback, hasJoin);
            return $"{col} {(s.Ascending ? "ASC" : "DESC")}";
        });
        return " ORDER BY " + string.Join(", ", parts);
    }

    private static void EmitDerivedSortSwitch(StringBuilder sb, EntityModel entity, string entityFqn, bool hasJoin)
    {
        var sortable = entity.DerivedQueryPaths.Where(p => p.IsSortable).ToList();
        sb.AppendLine("        var orderBy = (sort.Column, sort.Ascending) switch");
        sb.AppendLine("        {");
        foreach (var p in sortable)
        {
            var col = DerivedQueryPathBuilder.QualifyColumn(p, p.ColumnExpression, hasJoin);
            var dirAsc = col + " ASC";
            var dirDesc = col + " DESC";
            sb.AppendLine($"            (\"{p.PathKey}\", true)  => \" ORDER BY {dirAsc}\",");
            sb.AppendLine($"            (\"{p.PathKey}\", false) => \" ORDER BY {dirDesc}\",");
        }
        sb.AppendLine("            _ => throw new InvalidSortException(sort.Column),");
        sb.AppendLine("        };");
    }

    private static void EmitLockSuffixAppend(StringBuilder sb, string provider)
    {
        if (provider == "SqlServer")
        {
            sb.AppendLine("        if (lockMode is Dapper.Npa.Core.Enums.LockMode.Pessimistic or Dapper.Npa.Core.Enums.LockMode.PessimisticRead)");
            sb.AppendLine("        {");
            sb.AppendLine("            var lockHint = lockMode switch");
            sb.AppendLine("            {");
            sb.AppendLine("                Dapper.Npa.Core.Enums.LockMode.Pessimistic => \"WITH (UPDLOCK, ROWLOCK)\",");
            sb.AppendLine("                Dapper.Npa.Core.Enums.LockMode.PessimisticRead => \"WITH (HOLDLOCK, ROWLOCK)\",");
            sb.AppendLine("                _ => \"\",");
            sb.AppendLine("            };");
            sb.AppendLine("            sql = Dapper.Npa.Runtime.Query.SqlServerTableHint.Apply(sql, lockHint);");
            sb.AppendLine("        }");
            return;
        }

        sb.AppendLine("        sql += lockMode switch");
        sb.AppendLine("        {");
        if (provider == "Sqlite")
        {
            sb.AppendLine("            Dapper.Npa.Core.Enums.LockMode.Pessimistic => throw new NotSupportedException(\"Pessimistic lock is not supported on SQLite.\"),");
            sb.AppendLine("            Dapper.Npa.Core.Enums.LockMode.PessimisticRead => throw new NotSupportedException(\"Pessimistic read lock is not supported on SQLite.\"),");
        }
        else
        {
            var pessimistic = provider switch
            {
                "PostgreSql" => " FOR UPDATE",
                "MySql" => " FOR UPDATE",
                _ => " WITH (UPDLOCK, ROWLOCK)",
            };
            var pessimisticRead = provider switch
            {
                "PostgreSql" => " FOR SHARE",
                "MySql" => " FOR SHARE",
                _ => " WITH (HOLDLOCK, ROWLOCK)",
            };
            sb.AppendLine($"            Dapper.Npa.Core.Enums.LockMode.Pessimistic => \"{pessimistic}\",");
            sb.AppendLine($"            Dapper.Npa.Core.Enums.LockMode.PessimisticRead => \"{pessimisticRead}\",");
        }
        sb.AppendLine("            _ => \"\",");
        sb.AppendLine("        };");
    }

    private static void EmitDerivedSelectSqlVariables(
        StringBuilder sb,
        EntityModel entity,
        string selectCore,
        string whereSql,
        string orderSql,
        ParsedDerivedQuery parsed,
        bool hasIncludeDeleted,
        string provider,
        bool hasSort,
        bool hasJoin,
        bool deferSqlAssignment = false)
    {
        var activeOnly = BuildFullSelect(entity, selectCore, whereSql, orderSql, parsed, applySoftDelete: true, provider, hasJoin);
        if (hasIncludeDeleted && entity.SoftDeleteColumn is not null)
        {
            var includingDeleted = BuildFullSelect(entity, selectCore, whereSql, orderSql, parsed, applySoftDelete: false, provider, hasJoin);
            sb.AppendLine($"        const string sqlActiveOnly = \"{Esc(activeOnly)}\";");
            sb.AppendLine($"        const string sqlIncludingDeleted = \"{Esc(includingDeleted)}\";");
            sb.AppendLine("        var baseSql = includeDeleted ? sqlIncludingDeleted : sqlActiveOnly;");
        }
        else
        {
            sb.AppendLine($"        const string baseSql = \"{Esc(activeOnly)}\";");
        }

        if (!hasSort && !deferSqlAssignment)
        {
            sb.AppendLine("        var sql = baseSql;");
            EmitApplyGlobalFiltersIfNeeded(sb, entity);
        }
    }

    private static void EmitApplyGlobalFiltersIfNeeded(StringBuilder sb, EntityModel entity, string variableName = "sql")
    {
        if (!entity.GlobalFilters.Any())
            return;
        sb.AppendLine($"        {variableName} = ApplyGlobalFilters({variableName});");
    }

    private static void EmitDerivedCountSqlVariable(
        StringBuilder sb,
        EntityModel entity,
        string whereSql,
        bool hasIncludeDeleted,
        string provider,
        string variableName,
        string? countCore = null,
        ParsedDerivedQuery? parsed = null,
        bool hasJoin = false)
    {
        if (parsed is not null && hasJoin)
        {
            var from = BuildFromWithJoins(entity, CollectUsedPaths(entity, parsed, includeSortablePaths: false));
            countCore ??= $"SELECT COUNT(*) FROM {from}";
        }
        else
        {
            countCore ??= $"SELECT COUNT(*) FROM {entity.TableName}";
        }

        EmitPairedFilterSqlVariable(sb, entity, whereSql, hasIncludeDeleted, variableName, hasJoin,
            applySoftDelete => string.IsNullOrEmpty(BuildWhereClause(entity, whereSql, applySoftDelete, provider, hasJoin))
                ? countCore!
                : $"{countCore} WHERE {BuildWhereClause(entity, whereSql, applySoftDelete, provider, hasJoin)}");
    }

    private static void EmitDerivedExistsSqlVariable(
        StringBuilder sb,
        EntityModel entity,
        string whereSql,
        bool hasIncludeDeleted,
        string provider,
        string variableName,
        ParsedDerivedQuery parsed,
        bool hasJoin)
    {
        EmitPairedFilterSqlVariable(sb, entity, whereSql, hasIncludeDeleted, variableName, hasJoin, applySoftDelete =>
        {
            var innerWhere = BuildWhereClause(entity, whereSql, applySoftDelete, provider, hasJoin);
            string inner;
            if (hasJoin)
            {
                var from = BuildFromWithJoins(entity, CollectUsedPaths(entity, parsed, includeSortablePaths: false));
                inner = string.IsNullOrEmpty(innerWhere)
                    ? $"SELECT 1 FROM {from}"
                    : $"SELECT 1 FROM {from} WHERE {innerWhere}";
            }
            else
            {
                inner = string.IsNullOrEmpty(innerWhere)
                    ? $"SELECT 1 FROM {entity.TableName}"
                    : $"SELECT 1 FROM {entity.TableName} WHERE {innerWhere}";
            }
            return $"SELECT CASE WHEN EXISTS ({inner}) THEN 1 ELSE 0 END";
        });
    }

    private static void EmitPairedFilterSqlVariable(
        StringBuilder sb,
        EntityModel entity,
        string whereSql,
        bool hasIncludeDeleted,
        string variableName,
        bool hasJoin,
        Func<bool, string> buildSql)
    {
        var activeOnly = buildSql(true);
        if (hasIncludeDeleted && entity.SoftDeleteColumn is not null)
        {
            var includingDeleted = buildSql(false);
            sb.AppendLine($"        const string {variableName}ActiveOnly = \"{Esc(activeOnly)}\";");
            sb.AppendLine($"        const string {variableName}IncludingDeleted = \"{Esc(includingDeleted)}\";");
            sb.AppendLine($"        var {variableName} = includeDeleted ? {variableName}IncludingDeleted : {variableName}ActiveOnly;");
        }
        else
        {
            sb.AppendLine($"        const string {variableName} = \"{Esc(activeOnly)}\";");
        }
    }

    private static string BuildWhereClause(EntityModel entity, string whereSql, bool applySoftDelete, string provider, bool hasJoin = false)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(whereSql))
            parts.Add(whereSql);
        if (applySoftDelete && entity.SoftDeleteColumn is not null)
        {
            var sdCol = hasJoin ? $"e.{entity.SoftDeleteColumn}" : entity.SoftDeleteColumn;
            parts.Add(ProviderSqlHelper.SoftDeleteActivePredicate(sdCol, provider));
        }
        if (entity.TenantIdColumn is not null)
        {
            var tenantCol = hasJoin ? $"e.{entity.TenantIdColumn}" : entity.TenantIdColumn;
            parts.Add($"{tenantCol} = @tenantId");
        }
        return parts.Count == 0 ? string.Empty : string.Join(" AND ", parts);
    }

    private static string BuildFullSelect(
        EntityModel entity,
        string selectCore,
        string whereSql,
        string orderSql,
        ParsedDerivedQuery parsed,
        bool applySoftDelete,
        string provider,
        bool hasJoin = false)
    {
        var sql = selectCore;
        var where = BuildWhereClause(entity, whereSql, applySoftDelete, provider, hasJoin);
        if (!string.IsNullOrEmpty(where))
            sql += " WHERE " + where;
        if (!string.IsNullOrEmpty(orderSql))
            sql += orderSql;
        if (parsed.LimitN.HasValue)
            sql += $" OFFSET 0 ROWS FETCH NEXT {parsed.LimitN} ROWS ONLY";
        if (parsed.Distinct && !sql.Contains("SELECT DISTINCT"))
            sql = sql.Replace("SELECT ", "SELECT DISTINCT ");
        return sql;
    }

    private static string BuildParams(IMethodSymbol method, ParsedDerivedQuery parsed, EntityModel entity)
    {
        var valueParams = method.Parameters.Where(p => !IsRuntimeParameter(p)).ToList();
        var parts = new List<string>();
        var paramIndex = 0;

        foreach (var condition in parsed.Conditions.Where(c => c.ParameterCount > 0))
        {
            if (condition.ParameterCount == 2)
            {
                if (paramIndex + 1 >= valueParams.Count)
                    break;
                parts.Add(valueParams[paramIndex].Name);
                parts.Add(valueParams[paramIndex + 1].Name);
                paramIndex += 2;
                continue;
            }

            if (paramIndex >= valueParams.Count)
                break;
            parts.Add(valueParams[paramIndex].Name);
            paramIndex++;
        }

        if (entity.TenantIdColumn is not null)
            parts.Add("tenantId = _tenantProvider?.GetCurrentTenantId()");

        var joined = string.Join(", ", parts.Distinct());
        return string.IsNullOrEmpty(joined) ? string.Empty : joined + ", ";
    }

    private static void EmitEntityGraphSqlSelection(
        StringBuilder sb,
        EntityModel entity,
        string whereSql,
        bool hasIncludeDeleted,
        string provider,
        bool hasSort)
    {
        var graphWhereActive = BuildWhereClause(entity, whereSql, applySoftDelete: true, provider, hasJoin: true);
        if (hasIncludeDeleted && entity.SoftDeleteColumn is not null)
        {
            var graphWhereIncluding = BuildWhereClause(entity, whereSql, applySoftDelete: false, provider, hasJoin: true);
            sb.AppendLine($"        const string graphWhereActive = \"{Esc(graphWhereActive)}\";");
            sb.AppendLine($"        const string graphWhereIncludingDeleted = \"{Esc(graphWhereIncluding)}\";");
            sb.AppendLine("        var graphWhere = includeDeleted ? graphWhereIncludingDeleted : graphWhereActive;");
        }
        else
        {
            sb.AppendLine($"        const string graphWhere = \"{Esc(graphWhereActive)}\";");
        }

        if (!hasSort)
        {
            sb.AppendLine("        var sql = entityGraph is null");
            sb.AppendLine("            ? baseSql");
            sb.AppendLine("            : ResolveNamedEntityGraphFromSql(entityGraph) + (string.IsNullOrEmpty(graphWhere) ? string.Empty : \" WHERE \" + graphWhere);");
        }
        else
        {
            sb.AppendLine("        var graphSql = entityGraph is null");
            sb.AppendLine("            ? string.Empty");
            sb.AppendLine("            : ResolveNamedEntityGraphFromSql(entityGraph) + (string.IsNullOrEmpty(graphWhere) ? string.Empty : \" WHERE \" + graphWhere);");
        }
    }

    private static bool IsRuntimeParameter(IParameterSymbol p)
        => p.Name is "transaction" or "ct" or "includeDeleted"
           || p.Name.Equals("entityGraph", StringComparison.OrdinalIgnoreCase)
           || p.Name.Equals("EntityGraph", StringComparison.OrdinalIgnoreCase)
           || p.Type.Name is "Sort" or "Pageable" or "LockMode";

    private static string TransactionExpression(IMethodSymbol method)
        => method.Parameters.Any(p => p.Name == "transaction") ? "transaction" : "null";

    private static string BuildSignature(IMethodSymbol method, string entityFqn)
    {
        var returnType = method.ReturnType.ToDisplayString();
        var parts = method.Parameters
            .Select(p => $"{p.Type.ToDisplayString()} {p.Name}")
            .ToList();
        // Must match the interface member exactly — optional transaction/ct break implicit implementation.
        var parameters = string.Join(", ", parts);
        return $"public async {returnType} {method.Name}({parameters})";
    }

    private static string Esc(string s) => s.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
