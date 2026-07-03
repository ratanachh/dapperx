using DapperX.Generator.Cpql;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.Cpql;
using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class SoftDeleteValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (entity.SoftDeleteColumn is null)
            return;

        var columnExists = entity.Properties.Any(p =>
            string.Equals(p.ColumnName, entity.SoftDeleteColumn, StringComparison.OrdinalIgnoreCase));

        if (!columnExists)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.SoftDeleteColumnNotFound,
                symbol.Locations.FirstOrDefault(),
                entity.ClassName,
                entity.SoftDeleteColumn));
        }

        ScanRepositoryCpqlMethods(entity, symbol, ctx);
    }

    public static bool ValidateCpqlBypass(
        CpqlStatementNode ast,
        EntityModel entity,
        SourceProductionContext ctx,
        Location? location)
    {
        if (entity.SoftDeleteColumn is null)
            return true;

        var softNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            entity.SoftDeleteColumn,
        };
        foreach (var prop in entity.Properties)
        {
            if (string.Equals(prop.ColumnName, entity.SoftDeleteColumn, StringComparison.OrdinalIgnoreCase))
                softNames.Add(prop.PropertyName);
        }

        var found = false;
        void WalkValue(CpqlValueNode? n)
        {
            if (n is null) return;
            if (n is CpqlPropertyPathNode path && ReferencesSoftDelete(path, softNames))
                found = true;
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
            else if (n is CpqlNewExprNode ne) foreach (var a in ne.Arguments) WalkValue(a);
        }

        void WalkCond(CpqlConditionNode? c)
        {
            if (c is null) return;
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

        if (ast.Select is not null) WalkSelect(ast.Select);
        if (ast.Update is not null)
        {
            foreach (var a in ast.Update.Assignments)
            {
                if (softNames.Contains(a.Property))
                    found = true;
                WalkValue(a.Value);
            }
            if (ast.Update.Where is not null) WalkCond(ast.Update.Where);
        }
        if (ast.Delete?.Where is not null)
            WalkCond(ast.Delete.Where);

        if (!found)
            return true;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.CpqlSoftDeleteBypass,
            location,
            entity.SoftDeleteColumn,
            entity.ClassName));
        return false;
    }

    private static bool ReferencesSoftDelete(CpqlPropertyPathNode path, HashSet<string> softNames)
    {
        if (path.Path.Count == 0)
            return false;
        return softNames.Contains(path.Path[path.Path.Count - 1]);
    }

    private static void ScanRepositoryCpqlMethods(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        foreach (var query in entity.NamedQueries)
        {
            if (!query.Query.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.CpqlDeleteBypassesSoftDelete,
                symbol.Locations.FirstOrDefault(),
                query.Name,
                entity.ClassName));
        }

        foreach (var member in symbol.GetMembers().OfType<IMethodSymbol>())
        {
            var queryAttr = SyntaxHelper.GetAttribute(member, SyntaxHelper.QueryAttr);
            if (queryAttr is null)
                continue;

            var isNative = SyntaxHelper.GetNamedArg<bool?>(queryAttr, "NativeQuery") ?? false;
            if (isNative)
                continue;

            var cpql = SyntaxHelper.GetConstructorArg<string>(queryAttr, 0) ?? string.Empty;
            if (cpql.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CpqlDeleteBypassesSoftDelete,
                    member.Locations.FirstOrDefault(),
                    member.Name,
                    entity.ClassName));
            }
        }
    }
}
