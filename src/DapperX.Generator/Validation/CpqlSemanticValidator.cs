using DapperX.Generator.Cpql;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;

namespace DapperX.Generator.Validation;

internal static class CpqlSemanticValidator
{
    public static bool Validate(
        CpqlStatementNode ast,
        EntityModel rootEntity,
        IMethodSymbol method,
        IReadOnlyDictionary<string, EntityModel> allModels,
        Compilation compilation,
        SourceProductionContext ctx,
        Location? location)
    {
        var semantic = new CpqlSemanticContext
        {
            RootEntity = rootEntity,
            Method = method,
            Compilation = compilation,
            AllModels = allModels,
        };

        if (ast.With is not null)
        {
            foreach (var cte in ast.With.Ctes)
                semantic.CteNames.Add(cte.Name);
        }

        var ok = true;
        if (ast.Select is not null)
            ok &= ValidateSelect(ast.Select, ast.With, semantic, ctx, location, isRoot: true);
        if (ast.Update is not null)
            ok &= ValidateUpdate(ast.Update, semantic, ctx, location);
        if (ast.Delete is not null)
            ok &= ValidateDelete(ast.Delete, semantic, ctx, location);

        foreach (var cteName in semantic.CteNames)
        {
            if (!semantic.ReferencedCtes.Contains(cteName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("DPXCPQL030", "Unreferenced CTE",
                        "CTE '{0}' is never referenced in the query", "DapperX.CPQL",
                        DiagnosticSeverity.Warning, true),
                    location, cteName));
            }
        }

        return ok;
    }

    private static bool ValidateSelect(
        CpqlSelectStatementNode sel,
        CpqlWithClauseNode? with,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location,
        bool isRoot)
    {
        RegisterFrom(sel.From, semantic);
        foreach (var j in sel.Joins)
            RegisterJoin(j, semantic, ctx, location);

        if (with is not null)
        {
            foreach (var cte in with.Ctes)
            {
                ValidateSelect(cte.Body, null, semantic, ctx, location, isRoot: false);
                semantic.CteColumns[cte.Name] = ExtractCteColumns(cte.Body);
            }
        }

        var ok = true;
        var hasAggregate = false;
        var hasNonAggregate = false;

        foreach (var item in sel.SelectList.Items)
        {
            if (IsAggregateValue(item.Value))
                hasAggregate = true;
            else if (item.Value is not CpqlPropertyPathNode { Path.Count: 0 })
                hasNonAggregate = true;
            ok &= ValidateValue(item.Value, semantic, ctx, location, inWhereOrHaving: false);
        }

        if (hasAggregate && hasNonAggregate && sel.GroupBy.Count == 0)
        {
            ok &= Report(ctx, location, "DPXCPQL020",
                "SELECT mixes aggregate and non-aggregate expressions without GROUP BY");
        }

        if (sel.Having is not null && sel.GroupBy.Count == 0)
        {
            ok &= Report(ctx, location, "DPXCPQL021",
                "HAVING requires GROUP BY");
        }

        if (sel.Where is not null)
            ok &= ValidateCondition(sel.Where, semantic, ctx, location, inWhereOrHaving: true);
        if (sel.Having is not null)
            ok &= ValidateCondition(sel.Having, semantic, ctx, location, inWhereOrHaving: true);

        foreach (var g in sel.GroupBy)
            ok &= ValidateValue(g, semantic, ctx, location, inWhereOrHaving: false);
        foreach (var o in sel.OrderBy)
            ok &= ValidateValue(o.Value, semantic, ctx, location, inWhereOrHaving: false);

        if (isRoot)
            ok &= ValidateReturnType(sel, semantic, ctx, location);

        return ok;
    }

    private static bool ValidateUpdate(
        CpqlUpdateStatementNode upd,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var entity = semantic.ResolveEntityByName(upd.EntityName);
        if (entity is null)
        {
            okReport(ctx, location, "DPXCPQL001", $"Unknown entity '{upd.EntityName}' in UPDATE");
            return false;
        }

        semantic.Aliases[upd.Alias] = entity;
        var ok = true;
        foreach (var j in upd.Joins)
            ok &= RegisterJoin(j, semantic, ctx, location);
        foreach (var a in upd.Assignments)
        {
            ok &= ValidateProperty(upd.Alias, a.Property, semantic, ctx, location);
            ok &= ValidateValue(a.Value, semantic, ctx, location, inWhereOrHaving: false);
        }
        if (upd.Where is not null)
            ok &= ValidateCondition(upd.Where, semantic, ctx, location, inWhereOrHaving: true);

        var ret = semantic.Method.ReturnType.ToDisplayString();
        if (upd.Joins.Count > 0 && !ret.Contains("Task<int>", StringComparison.Ordinal))
        {
            ok &= Report(ctx, location, "DPXCPQL022",
                "Bulk UPDATE with JOIN requires return type Task<int>");
        }

        return ok;
    }

    private static bool ValidateDelete(
        CpqlDeleteStatementNode del,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var entity = semantic.ResolveEntityByName(del.EntityName);
        if (entity is null)
        {
            okReport(ctx, location, "DPXCPQL001", $"Unknown entity '{del.EntityName}' in DELETE");
            return false;
        }

        semantic.Aliases[del.Alias] = entity;
        var ok = true;
        if (del.Where is not null)
            ok &= ValidateCondition(del.Where, semantic, ctx, location, inWhereOrHaving: true);

        var ret = semantic.Method.ReturnType.ToDisplayString();
        if (!ret.Contains("Task<int>", StringComparison.Ordinal))
        {
            ok &= Report(ctx, location, "DPXCPQL022",
                "Bulk DELETE requires return type Task<int>");
        }

        return ok;
    }

    private static void RegisterFrom(CpqlFromNode from, CpqlSemanticContext semantic)
    {
        if (semantic.CteNames.Contains(from.EntityOrCteName))
        {
            from.IsCte = true;
            semantic.ReferencedCtes.Add(from.EntityOrCteName);
            if (semantic.CteColumns.TryGetValue(from.EntityOrCteName, out var cols))
                semantic.CteAliasColumns[from.Alias] = cols;
            return;
        }

        var entity = semantic.ResolveEntityByName(from.EntityOrCteName) ?? semantic.RootEntity;
        semantic.Aliases[from.Alias] = entity;
    }

    private static bool RegisterJoin(
        CpqlJoinNode join,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var source = semantic.GetAliasEntity(join.SourceAlias);
        if (source is null)
            return Report(ctx, location, "DPXCPQL002", $"Unknown alias '{join.SourceAlias}' in JOIN");

        var rel = source.Relationships.FirstOrDefault(r => r.PropertyName == join.RelationshipProperty);
        if (rel is null)
            return Report(ctx, location, "DPXCPQL003",
                $"Relationship '{join.RelationshipProperty}' not found on entity '{source.ClassName}'");

        var target = semantic.ResolveTargetEntity(rel);
        if (target is null)
            return Report(ctx, location, "DPXCPQL003",
                $"Cannot resolve target entity for relationship '{join.RelationshipProperty}'");

        semantic.Aliases[join.JoinAlias] = target;

        if (join.IsLeft)
        {
            var fkProp = target.Properties.FirstOrDefault(p =>
                p.ColumnName == rel.ForeignKeyColumn || p.PropertyName == rel.FkPropertyNameOnChild);
            if (fkProp is not null && !fkProp.Nullable && rel.ForeignKeyColumn is not null)
            {
                return Report(ctx, location, "DPXCPQL023",
                    $"LEFT JOIN on '{join.RelationshipProperty}' requires nullable foreign key");
            }
        }

        return true;
    }

    private static bool ValidateValue(
        CpqlValueNode node,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location,
        bool inWhereOrHaving)
    {
        var ok = true;
        switch (node)
        {
            case CpqlPropertyPathNode path:
                ok &= ValidatePropertyPath(path, semantic, ctx, location);
                break;
            case CpqlArithmeticNode ar:
                ok &= ValidateValue(ar.Left, semantic, ctx, location, inWhereOrHaving);
                ok &= ValidateValue(ar.Right, semantic, ctx, location, inWhereOrHaving);
                var lk = CpqlTypeHelper.InferValueKind(ar.Left, semantic);
                var rk = CpqlTypeHelper.InferValueKind(ar.Right, semantic);
                if (!CpqlTypeHelper.IsNumeric(lk) || !CpqlTypeHelper.IsNumeric(rk))
                {
                    if (lk != CpqlValueKind.Unknown && rk != CpqlValueKind.Unknown)
                        ok &= Report(ctx, location, "DPXCPQL024", "Arithmetic operands must be numeric");
                }
                break;
            case CpqlCaseNode c:
                ok &= ValidateCase(c, semantic, ctx, location);
                break;
            case CpqlNewExprNode n:
                ok &= ValidateNew(n, semantic, ctx, location);
                break;
            case CpqlScalarFunctionNode sf:
                ok &= ValidateScalarFunction(sf, semantic, ctx, location);
                break;
            case CpqlSubqueryNode sq:
                ok &= ValidateSubquerySelect(sq.Select, semantic, ctx, location, scalar: true);
                break;
            case CpqlWindowNode win:
                if (inWhereOrHaving)
                    ok &= Report(ctx, location, "DPXCPQL012", "Window functions are not allowed in WHERE or HAVING");
                ok &= ValidateValue(win.Inner, semantic, ctx, location, inWhereOrHaving);
                foreach (var p in win.PartitionBy)
                    ok &= ValidateValue(p, semantic, ctx, location, inWhereOrHaving);
                foreach (var o in win.OrderBy)
                    ok &= ValidateValue(o.Value, semantic, ctx, location, inWhereOrHaving);
                break;
            default:
                WalkNestedValues(node, semantic, ctx, location, inWhereOrHaving, ref ok);
                break;
        }
        return ok;
    }

    private static void WalkNestedValues(
        CpqlValueNode node,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location,
        bool inWhereOrHaving,
        ref bool ok)
    {
        if (node is CpqlAggregateNode ag && ag.Argument is not null)
            ok &= ValidateValue(ag.Argument, semantic, ctx, location, inWhereOrHaving);
    }

    private static bool ValidateCase(
        CpqlCaseNode c,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var ok = true;
        if (c.Input is not null)
            ok &= ValidateValue(c.Input, semantic, ctx, location, inWhereOrHaving: false);
        CpqlValueKind? branchKind = null;
        foreach (var w in c.Whens)
        {
            ok &= ValidateCondition(w.Condition, semantic, ctx, location, inWhereOrHaving: false);
            ok &= ValidateValue(w.Result, semantic, ctx, location, inWhereOrHaving: false);
            var k = CpqlTypeHelper.InferValueKind(w.Result, semantic);
            if (branchKind is null)
                branchKind = k;
            else if (!CpqlTypeHelper.AreCompatible(branchKind.Value, k))
                ok &= Report(ctx, location, "DPXCPQL025", "CASE branch value types must be compatible");
        }
        if (c.Else is null)
        {
            ok &= Report(ctx, location, "DPXCPQL026",
                "CASE without ELSE may return NULL — ensure return type is nullable",
                DiagnosticSeverity.Warning);
        }
        else
        {
            ok &= ValidateValue(c.Else, semantic, ctx, location, inWhereOrHaving: false);
        }
        return ok;
    }

    private static bool ValidateNew(
        CpqlNewExprNode n,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var ok = true;
        foreach (var arg in n.Arguments)
            ok &= ValidateValue(arg, semantic, ctx, location, inWhereOrHaving: false);

        var ctor = FindConstructorType(n.TypeName, semantic);
        if (ctor is null)
        {
            ok &= Report(ctx, location, "DPXCPQL027",
                $"Constructor type '{n.TypeName}' could not be resolved for NEW expression — verify type and parameter order");
            return ok;
        }

        var ctors = ctor.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic)
            .ToList();
        var matching = ctors.Where(c => c.Parameters.Length == n.Arguments.Count).ToList();
        if (matching.Count == 0)
        {
            ok &= Report(ctx, location, "DPXCPQL027",
                $"No public constructor on '{n.TypeName}' accepts {n.Arguments.Count} argument(s)");
            return ok;
        }

        if (matching.Count == 1)
        {
            var ctorParams = matching[0].Parameters;
            for (var i = 0; i < n.Arguments.Count; i++)
            {
                var argKind = CpqlTypeHelper.InferValueKind(n.Arguments[i], semantic);
                var paramType = ctorParams[i].Type;
                if (!ConstructorArgMatches(argKind, paramType))
                {
                    ok &= Report(ctx, location, "DPXCPQL027",
                        $"NEW argument {i + 1} type is not compatible with constructor parameter '{ctorParams[i].Name}'");
                }
            }
        }

        return ok;
    }

    private static bool ConstructorArgMatches(CpqlValueKind argKind, ITypeSymbol paramType)
    {
        if (argKind == CpqlValueKind.Unknown)
            return true;
        var display = paramType.ToDisplayString();
        return argKind switch
        {
            CpqlValueKind.String => display.Contains("string", StringComparison.Ordinal),
            CpqlValueKind.Numeric => display.Contains("int", StringComparison.Ordinal)
                || display.Contains("long", StringComparison.Ordinal)
                || display.Contains("decimal", StringComparison.Ordinal)
                || display.Contains("double", StringComparison.Ordinal),
            CpqlValueKind.Bool => display.Contains("bool", StringComparison.Ordinal),
            CpqlValueKind.DateTime => display.Contains("DateTime", StringComparison.Ordinal),
            CpqlValueKind.Null => paramType.IsReferenceType || paramType.NullableAnnotation == NullableAnnotation.Annotated,
            _ => true,
        };
    }

    private static INamedTypeSymbol? FindConstructorType(string typeName, CpqlSemanticContext semantic)
    {
        if (semantic.Compilation is not null)
        {
            var resolved = ResolveNamedType(semantic.Compilation, typeName, semantic.Method);
            if (resolved is not null)
                return resolved;
        }

        var method = semantic.Method;
        var candidate = method.ContainingNamespace.GetTypeMembers(typeName).FirstOrDefault();
        if (candidate is not null) return candidate;

        foreach (var ns in method.ContainingNamespace.GetNamespaceMembers())
        {
            candidate = ns.GetTypeMembers(typeName).FirstOrDefault();
            if (candidate is not null) return candidate;
        }

        return null;
    }

    private static INamedTypeSymbol? ResolveNamedType(Compilation compilation, string typeName, IMethodSymbol method)
    {
        var ns = method.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            var qualified = compilation.GetTypeByMetadataName($"{ns.ToDisplayString()}.{typeName}");
            if (qualified is not null) return qualified;
        }

        var found = WalkNamespace(compilation.Assembly.GlobalNamespace, typeName);
        if (found is not null)
            return found;

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol asm)
                continue;
            found = WalkNamespace(asm.GlobalNamespace, typeName);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static INamedTypeSymbol? WalkNamespace(INamespaceSymbol ns, string typeName)
    {
        var direct = ns.GetTypeMembers(typeName).FirstOrDefault();
        if (direct is not null) return direct;

        foreach (var child in ns.GetNamespaceMembers())
        {
            var found = WalkNamespace(child, typeName);
            if (found is not null) return found;
        }

        return null;
    }

    private static string? GetTaskInnerType(IMethodSymbol method)
    {
        if (method.ReturnType is not INamedTypeSymbol task || !task.IsGenericType)
            return null;
        return task.TypeArguments[0].ToDisplayString();
    }

    private static bool ValidateScalarFunction(
        CpqlScalarFunctionNode sf,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var ok = true;
        foreach (var arg in sf.Arguments)
            ok &= ValidateValue(arg, semantic, ctx, location, inWhereOrHaving: false);

        var name = sf.Name.ToUpperInvariant();
        var argKinds = sf.Arguments.Select(a => CpqlTypeHelper.InferValueKind(a, semantic)).ToList();

        if (name is "CONCAT")
        {
            if (argKinds.Any(k => !CpqlTypeHelper.IsStringCompatible(k) && k != CpqlValueKind.Null))
                ok &= Report(ctx, location, "DPXCPQL028", "CONCAT arguments must be string-compatible");
        }
        else if (name is "NULLIF" && argKinds.Count >= 2)
        {
            if (!CpqlTypeHelper.AreCompatible(argKinds[0], argKinds[1]))
                ok &= Report(ctx, location, "DPXCPQL029", "NULLIF arguments must be compatible types");
        }
        else if (name is "MOD" or "POWER")
        {
            if (argKinds.Any(k => !CpqlTypeHelper.IsNumeric(k) && k != CpqlValueKind.Unknown))
                ok &= Report(ctx, location, "DPXCPQL024", $"{name} operands must be numeric");
        }
        else if (name is "LEFT" or "RIGHT" or "SUBSTRING")
        {
            if (argKinds.Count > 0 && !CpqlTypeHelper.IsStringCompatible(argKinds[0]) && argKinds[0] != CpqlValueKind.Unknown)
                ok &= Report(ctx, location, "DPXCPQL028", $"{name} first operand must be string type");
        }
        else if (name is "YEAR" or "MONTH" or "DAY" or "HOUR" or "MINUTE" or "SECOND")
        {
            if (argKinds.Count > 0 && argKinds[0] is not (CpqlValueKind.DateTime or CpqlValueKind.Unknown))
                ok &= Report(ctx, location, "DPXCPQL034", $"{name} requires date/datetime argument");
        }

        if (name == "CAST" && sf.CastType is not null && sf.Arguments.Count > 0)
        {
            // Basic cast validation — source known types only
            var src = CpqlTypeHelper.InferValueKind(sf.Arguments[0], semantic);
            if (src == CpqlValueKind.Bool && sf.CastType is not ("BOOLEAN" or "BOOL"))
                ok &= Report(ctx, location, "DPXCPQL031", $"Invalid CAST from bool to {sf.CastType}");
        }

        return ok;
    }

    private static IReadOnlyList<string> ExtractCteColumns(CpqlSelectStatementNode sel)
    {
        var cols = new List<string>();
        foreach (var item in sel.SelectList.Items)
        {
            if (!string.IsNullOrEmpty(item.Alias))
            {
                cols.Add(item.Alias!);
                continue;
            }

            if (item.Value is CpqlPropertyPathNode path && path.Path.Count > 0)
                cols.Add(path.Path[path.Path.Count - 1]);
            else
                cols.Add($"col{cols.Count}");
        }
        return cols;
    }

    private static bool ValidatePropertyPath(
        CpqlPropertyPathNode path,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        if (path.Path.Count == 0)
            return semantic.GetAliasEntity(path.Alias) is not null
                || semantic.CteAliasColumns.ContainsKey(path.Alias)
                || Report(ctx, location, "DPXCPQL002", $"Unknown alias '{path.Alias}'");

        if (semantic.CteAliasColumns.TryGetValue(path.Alias, out var cteCols))
        {
            var colName = path.Path[path.Path.Count - 1];
            return cteCols.Any(c => string.Equals(c, colName, StringComparison.OrdinalIgnoreCase))
                || Report(ctx, location, "DPXCPQL004",
                    $"Column '{colName}' not found on CTE alias '{path.Alias}'");
        }

        var entity = semantic.GetAliasEntity(path.Alias);
        if (entity is null)
            return Report(ctx, location, "DPXCPQL002", $"Unknown alias '{path.Alias}'");

        for (var i = 0; i < path.Path.Count; i++)
        {
            var segment = path.Path[i];
            if (i < path.Path.Count - 1)
            {
                var rel = entity.Relationships.FirstOrDefault(r => r.PropertyName == segment);
                if (rel is null)
                    return Report(ctx, location, "DPXCPQL004",
                        $"Property '{segment}' is not a relationship on '{entity.ClassName}'");
                entity = semantic.ResolveTargetEntity(rel);
                if (entity is null)
                    return Report(ctx, location, "DPXCPQL004", $"Cannot resolve target for '{segment}'");
            }
            else
            {
                var leaf = entity.Properties.FirstOrDefault(p => p.PropertyName == segment);
                if (leaf is null)
                {
                    return Report(ctx, location, "DPXCPQL004",
                        $"Property '{segment}' not found on entity '{entity.ClassName}'");
                }

                if (leaf.Formula is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.FormulaNotQueryable,
                        location,
                        segment,
                        entity.ClassName));
                    return false;
                }
            }
        }
        return true;
    }

    private static bool ValidateProperty(
        string alias,
        string property,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
        => ValidatePropertyPath(new CpqlPropertyPathNode { Alias = alias, Path = { property } },
            semantic, ctx, location);

    private static bool ValidateCondition(
        CpqlConditionNode cond,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location,
        bool inWhereOrHaving)
    {
        var ok = true;
        switch (cond)
        {
            case CpqlAndNode a:
                ok &= ValidateCondition(a.Left, semantic, ctx, location, inWhereOrHaving);
                ok &= ValidateCondition(a.Right, semantic, ctx, location, inWhereOrHaving);
                break;
            case CpqlOrNode o:
                ok &= ValidateCondition(o.Left, semantic, ctx, location, inWhereOrHaving);
                ok &= ValidateCondition(o.Right, semantic, ctx, location, inWhereOrHaving);
                break;
            case CpqlNotNode n:
                ok &= ValidateCondition(n.Inner, semantic, ctx, location, inWhereOrHaving);
                break;
            case CpqlPredicateNode p:
                if (p.Left is not null)
                    ok &= ValidateValue(p.Left, semantic, ctx, location, inWhereOrHaving);
                if (p.Right is not null)
                    ok &= ValidateValue(p.Right, semantic, ctx, location, inWhereOrHaving);
                if (p.Right2 is not null)
                    ok &= ValidateValue(p.Right2, semantic, ctx, location, inWhereOrHaving);
                if (p.Subquery is not null)
                {
                    var scalar = p.Kind is CpqlPredicateKind.Comparison or CpqlPredicateKind.InSubquery;
                    ok &= ValidateSubquerySelect(p.Subquery.Select, semantic, ctx, location, scalar);
                    if (p.Kind is CpqlPredicateKind.InSubquery && p.Left is not null)
                    {
                        var leftKind = CpqlTypeHelper.InferValueKind(p.Left, semantic);
                        var subKind = InferSubqueryProjectionKind(p.Subquery.Select, semantic);
                        if (leftKind != CpqlValueKind.Unknown && subKind != CpqlValueKind.Unknown
                            && !CpqlTypeHelper.AreCompatible(leftKind, subKind))
                        {
                            ok &= Report(ctx, location, "DPXCPQL035",
                                "IN subquery column type must be compatible with left-hand expression");
                        }
                    }
                }
                break;
        }
        return ok;
    }

    private static bool ValidateSubquerySelect(
        CpqlSelectStatementNode sel,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location,
        bool scalar)
    {
        if (scalar && sel.SelectList.Items.Count != 1)
            return Report(ctx, location, "DPXCPQL032", "Scalar subquery must project exactly one column");
        if (!scalar && sel.SelectList.Items.Count != 1)
            return Report(ctx, location, "DPXCPQL032", "IN/EXISTS subquery must project exactly one column");

        var savedAliases = new Dictionary<string, EntityModel>(semantic.Aliases, StringComparer.Ordinal);
        var savedCteAlias = new Dictionary<string, IReadOnlyList<string>>(semantic.CteAliasColumns, StringComparer.Ordinal);
        var ok = ValidateSelect(sel, null, semantic, ctx, location, isRoot: false);
        semantic.Aliases.Clear();
        foreach (var kv in savedAliases)
            semantic.Aliases[kv.Key] = kv.Value;
        semantic.CteAliasColumns.Clear();
        foreach (var kv in savedCteAlias)
            semantic.CteAliasColumns[kv.Key] = kv.Value;
        return ok;
    }

    private static CpqlValueKind InferSubqueryProjectionKind(
        CpqlSelectStatementNode sel,
        CpqlSemanticContext semantic)
    {
        if (sel.SelectList.Items.Count != 1)
            return CpqlValueKind.Unknown;
        return CpqlTypeHelper.InferValueKind(sel.SelectList.Items[0].Value, semantic);
    }

    private static bool ValidateReturnType(
        CpqlSelectStatementNode sel,
        CpqlSemanticContext semantic,
        SourceProductionContext ctx,
        Location? location)
    {
        var ret = semantic.Method.ReturnType;
        if (ret is not INamedTypeSymbol task || !task.IsGenericType)
            return true;

        var inner = task.TypeArguments[0];
        var innerDisplay = inner.ToDisplayString();

        if (inner.SpecialType == SpecialType.System_Boolean)
            return true;

        if (inner.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64)
        {
            if (sel.SelectList.Items.Count == 1 && IsAggregateValue(sel.SelectList.Items[0].Value))
                return true;
            return Report(ctx, location, "DPXCPQL033",
                "Numeric return type requires aggregate SELECT (e.g. COUNT)");
        }

        if (sel.SelectList.Items.Count == 1 && sel.SelectList.Items[0].Value is CpqlNewExprNode n)
        {
            var ctorType = FindConstructorType(n.TypeName, semantic);
            if (ctorType is not null && !SymbolEqualityComparer.Default.Equals(ctorType, inner))
            {
                return Report(ctx, location, "DPXCPQL033",
                    $"Return type '{innerDisplay}' does not match NEW projection type '{ctorType.ToDisplayString()}'");
            }
            return true;
        }

        if (inner is INamedTypeSymbol named
            && string.Equals(named.ToDisplayString(), semantic.RootEntity.FullyQualifiedName, StringComparison.Ordinal))
            return true;

        if (inner is INamedTypeSymbol enumerable
            && enumerable.IsGenericType
            && enumerable.TypeArguments.Length == 1
            && enumerable.TypeArguments[0] is INamedTypeSymbol elem
            && string.Equals(elem.ToDisplayString(), semantic.RootEntity.FullyQualifiedName, StringComparison.Ordinal))
            return true;

        if (sel.SelectList.Items.Count == 1 && IsAggregateValue(sel.SelectList.Items[0].Value))
            return true;

        return true;
    }

    private static bool IsAggregateValue(CpqlValueNode node)
        => node is CpqlAggregateNode or CpqlWindowNode
           || (node is CpqlScalarFunctionNode sf && sf.Name.Equals("COUNT", StringComparison.OrdinalIgnoreCase));

    private static bool Report(SourceProductionContext ctx, Location? loc, string id, string msg,
        DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, "CPQL validation", msg, "DapperX.CPQL", severity, true), loc));
        return severity != DiagnosticSeverity.Error;
    }

    private static void okReport(SourceProductionContext ctx, Location? loc, string id, string msg)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, "CPQL validation", msg, "DapperX.CPQL",
                DiagnosticSeverity.Error, true), loc));
    }
}
