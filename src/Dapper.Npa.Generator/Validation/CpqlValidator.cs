using Dapper.Npa.Generator.Cpql;
using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;
using Microsoft.CodeAnalysis;

namespace Dapper.Npa.Generator.Validation;

internal static class CpqlValidator
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
        var ok = true;
        var usedParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectParameters(ast, usedParams);

        foreach (var p in usedParams)
        {
            if (method.Parameters.All(x => !string.Equals(x.Name, p, StringComparison.OrdinalIgnoreCase)))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("DPXCPQL010", "CPQL parameter not found",
                        "CPQL parameter ':{0}' does not match any method parameter", "Dapper.Npa.CPQL",
                        DiagnosticSeverity.Error, true),
                    location, p));
                ok = false;
            }
        }

        foreach (var p in method.Parameters)
        {
            if (p.Name is null) continue;
            if (!usedParams.Contains(p.Name)
                && p.Type.Name is not "CancellationToken"
                && p.Type.Name is not "IDbTransaction"
                && p.Type.Name is not "Sort"
                && p.Type.Name is not "Pageable"
                && p.Type.Name is not "LockMode"
                && p.Name is not "includeDeleted")
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("DPXCPQL011", "Unused CPQL parameter",
                        "Method parameter '{0}' is not referenced in CPQL", "Dapper.Npa.CPQL",
                        DiagnosticSeverity.Warning, true),
                    location, p.Name));
            }
        }

        if (rootEntity.IsImmutable && ast.Update is not null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MutatingMethodOnImmutable,
                location, rootEntity.ClassName, method.Name));
            ok = false;
        }

        if (ast.Delete is not null && rootEntity.SoftDeleteColumn is not null)
        {
            // soft-delete entities translate DELETE to UPDATE — allowed
        }

        if (!WalkForWindows(ast, ctx, location)) ok = false;
        if (!WalkForNestedSubqueries(ast, ctx, location)) ok = false;
        if (!CpqlSemanticValidator.Validate(ast, rootEntity, method, allModels, compilation, ctx, location))
            ok = false;

        if (!SoftDeleteValidator.ValidateCpqlBypass(ast, rootEntity, ctx, location))
            ok = false;

        return ok;
    }

    private static void CollectParameters(CpqlStatementNode stmt, HashSet<string> used)
    {
        void WalkValue(CpqlValueNode? n)
        {
            if (n is null) return;
            if (n is CpqlParameterNode p) used.Add(p.Name);
            else if (n is CpqlArithmeticNode ar) { WalkValue(ar.Left); WalkValue(ar.Right); }
            else if (n is CpqlCaseNode c)
            {
                WalkValue(c.Input);
                foreach (var w in c.Whens) WalkCond(w.Condition);
                WalkValue(c.Else);
            }
            else if (n is CpqlAggregateNode ag) WalkValue(ag.Argument);
            else if (n is CpqlScalarFunctionNode sf) foreach (var a in sf.Arguments) WalkValue(a);
            else if (n is CpqlWindowNode w) { WalkValue(w.Inner); foreach (var x in w.PartitionBy) WalkValue(x); }
            else if (n is CpqlSubqueryNode sq) WalkSelect(sq.Select);
        }

        void WalkCond(CpqlConditionNode? c)
        {
            if (c is CpqlAndNode a) { WalkCond(a.Left); WalkCond(a.Right); }
            else if (c is CpqlOrNode o) { WalkCond(o.Left); WalkCond(o.Right); }
            else if (c is CpqlNotNode n) WalkCond(n.Inner);
            else if (c is CpqlPredicateNode p)
            {
                WalkValue(p.Left);
                WalkValue(p.Right);
                WalkValue(p.Right2);
                if (p.Subquery is not null) WalkSelect(p.Subquery.Select);
            }
        }

        void WalkSelect(CpqlSelectStatementNode sel)
        {
            foreach (var item in sel.SelectList.Items) WalkValue(item.Value);
            if (sel.Where is not null) WalkCond(sel.Where);
            foreach (var g in sel.GroupBy) WalkValue(g);
            if (sel.Having is not null) WalkCond(sel.Having);
            foreach (var o in sel.OrderBy) WalkValue(o.Value);
        }

        if (stmt.Select is not null) WalkSelect(stmt.Select);
        if (stmt.Update is not null)
        {
            foreach (var a in stmt.Update.Assignments) WalkValue(a.Value);
            if (stmt.Update.Where is not null) WalkCond(stmt.Update.Where);
        }
        if (stmt.Delete?.Where is not null) WalkCond(stmt.Delete.Where);
    }

    private static bool WalkForWindows(CpqlStatementNode stmt, SourceProductionContext ctx, Location? loc)
    {
        var ok = true;
        void CheckValue(CpqlValueNode n, bool inWhereOrHaving)
        {
            if (n is CpqlWindowNode && inWhereOrHaving)
            {
                if (Report(ctx, loc, "Window functions are not allowed in WHERE or HAVING")) ok = false;
            }
            if (n is CpqlWindowNode win)
                CheckValue(win.Inner, inWhereOrHaving);
            else if (n is CpqlArithmeticNode ar)
            {
                CheckValue(ar.Left, inWhereOrHaving);
                CheckValue(ar.Right, inWhereOrHaving);
            }
        }

        if (stmt.Select?.Where is not null)
            WalkCond(stmt.Select.Where, true);
        if (stmt.Select?.Having is not null)
            WalkCond(stmt.Select.Having, true);

        void WalkCond(CpqlConditionNode c, bool inWh)
        {
            if (c is CpqlAndNode a) { WalkCond(a.Left, inWh); WalkCond(a.Right, inWh); }
            else if (c is CpqlOrNode o) { WalkCond(o.Left, inWh); WalkCond(o.Right, inWh); }
            else if (c is CpqlNotNode n) WalkCond(n.Inner, inWh);
            else if (c is CpqlPredicateNode p)
            {
                if (p.Left is not null) CheckValue(p.Left, inWh);
                if (p.Right is not null) CheckValue(p.Right, inWh);
            }
        }

        return ok;
    }

    private static bool WalkForNestedSubqueries(CpqlStatementNode stmt, SourceProductionContext ctx, Location? loc)
    {
        var ok = true;
        void WalkSelect(CpqlSelectStatementNode sel, int depth)
        {
            void WalkCond(CpqlConditionNode c)
            {
                if (c is CpqlAndNode a) { WalkCond(a.Left); WalkCond(a.Right); }
                else if (c is CpqlOrNode o) { WalkCond(o.Left); WalkCond(o.Right); }
                else if (c is CpqlNotNode n) WalkCond(n.Inner);
                else if (c is CpqlPredicateNode p && p.Subquery is not null)
                {
                    if (depth >= 1 && Report(ctx, loc, "Nested subqueries are not allowed")) ok = false;
                    WalkSelect(p.Subquery.Select, depth + 1);
                }
            }
            if (sel.Where is not null) WalkCond(sel.Where);
            foreach (var item in sel.SelectList.Items)
            {
                if (item.Value is CpqlSubqueryNode sq)
                {
                    if (depth >= 1 && Report(ctx, loc, "Nested subqueries are not allowed")) ok = false;
                    WalkSelect(sq.Select, depth + 1);
                }
            }
        }
        if (stmt.Select is not null) WalkSelect(stmt.Select, 0);
        return ok;
    }

    private static bool Report(SourceProductionContext ctx, Location? loc, string msg)
    {
        ctx.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor("DPXCPQL012", "CPQL validation", msg, "Dapper.Npa.CPQL",
                DiagnosticSeverity.Error, true), loc));
        return false;
    }
}
