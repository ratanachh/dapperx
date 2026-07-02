using System.Text;
using Dapper.Npa.Generator.Generators;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Cpql;

internal sealed class CpqlTranslator
{
    private readonly CpqlTranslationContext _ctx;
    private readonly StringBuilder _sb = new();

    public CpqlTranslator(CpqlTranslationContext ctx) => _ctx = ctx;

    public static string Translate(CpqlStatementNode stmt, CpqlTranslationContext ctx)
    {
        var t = new CpqlTranslator(ctx);
        return t.TranslateStatement(stmt);
    }

    public static string TranslateCount(CpqlStatementNode stmt, CpqlTranslationContext ctx)
    {
        if (stmt.Select is null)
            throw new InvalidOperationException("COUNT translation requires SELECT statement");
        var t = new CpqlTranslator(ctx);
        return t.TranslateSelectCount(stmt.Select, stmt.With);
    }

    private string TranslateStatement(CpqlStatementNode stmt)
    {
        if (stmt.With is not null)
        {
            foreach (var cte in stmt.With.Ctes)
                _ctx.CteNames[cte.Name] = cte.Name;
        }

        if (stmt.Select is not null)
            return TranslateSelect(stmt.Select, stmt.With);
        if (stmt.Update is not null)
            return TranslateUpdate(stmt.Update);
        if (stmt.Delete is not null)
            return TranslateDelete(stmt.Delete);
        throw new InvalidOperationException("Empty CPQL statement");
    }

    private string TranslateSelect(CpqlSelectStatementNode sel, CpqlWithClauseNode? with)
    {
        RegisterFrom(sel.From);
        foreach (var j in sel.Joins)
            RegisterExplicitJoin(j);
        CollectImplicitJoins(sel);

        _sb.Clear();
        if (with is not null)
            EmitWith(with);

        _sb.Append("SELECT ");
        if (sel.Distinct) _sb.Append("DISTINCT ");
        _sb.Append(TranslateSelectList(sel.SelectList));
        _sb.Append(" FROM ");
        _sb.Append(FormatTableRef(sel.From));
        EmitAllJoins();

        var whereParts = new List<string>();
        if (sel.Where is not null)
            whereParts.Add(TranslateCondition(sel.Where));
        AppendEntityFilters(sel.From.Alias, whereParts);

        if (whereParts.Count > 0)
            _sb.Append(" WHERE ").Append(string.Join(" AND ", whereParts));

        if (sel.GroupBy.Count > 0)
        {
            _sb.Append(" GROUP BY ");
            _sb.Append(string.Join(", ", sel.GroupBy.Select(TranslateValue)));
        }
        if (sel.Having is not null)
            _sb.Append(" HAVING ").Append(TranslateCondition(sel.Having));
        if (sel.OrderBy.Count > 0)
        {
            _sb.Append(" ORDER BY ");
            _sb.Append(string.Join(", ", sel.OrderBy.Select(TranslateOrderItem)));
        }
        return _sb.ToString();
    }

    private string TranslateSelectCount(CpqlSelectStatementNode sel, CpqlWithClauseNode? with)
    {
        RegisterFrom(sel.From);
        foreach (var j in sel.Joins)
            RegisterExplicitJoin(j);
        CollectImplicitJoins(sel);

        _sb.Clear();
        if (with is not null)
            EmitWith(with);

        _sb.Append("SELECT COUNT(*)");
        _sb.Append(" FROM ");
        _sb.Append(FormatTableRef(sel.From));
        EmitAllJoins();

        var whereParts = new List<string>();
        if (sel.Where is not null)
            whereParts.Add(TranslateCondition(sel.Where));
        AppendEntityFilters(sel.From.Alias, whereParts);
        if (whereParts.Count > 0)
            _sb.Append(" WHERE ").Append(string.Join(" AND ", whereParts));
        return _sb.ToString();
    }

    private void EmitWith(CpqlWithClauseNode with)
    {
        _sb.Append(with.Recursive ? "WITH RECURSIVE " : "WITH ");
        for (var i = 0; i < with.Ctes.Count; i++)
        {
            if (i > 0) _sb.Append(", ");
            var cte = with.Ctes[i];
            _sb.Append(cte.Name).Append(" AS (");
            var inner = TranslateSelect(cte.Body, null);
            _sb.Append(inner).Append(')');
        }
        _sb.Append(' ');
    }

    private string TranslateSelectList(CpqlSelectListNode list)
    {
        var items = new List<string>();
        foreach (var item in list.Items)
        {
            var expr = TranslateValue(item.Value);
            if (!string.IsNullOrEmpty(item.Alias))
                expr += " AS " + item.Alias;
            items.Add(expr);
        }
        return string.Join(", ", items);
    }

    private string TranslateUpdate(CpqlUpdateStatementNode upd)
    {
        var entity = _ctx.ResolveEntityByName(upd.EntityName)
            ?? throw new InvalidOperationException("Unknown entity " + upd.EntityName);
        _ctx.Aliases[upd.Alias] = entity;

        foreach (var j in upd.Joins)
            RegisterExplicitJoin(j);

        _sb.Clear();
        var setClause = string.Join(", ", upd.Assignments.Select(a =>
            $"{upd.Alias}.{ResolveColumn(upd.Alias, a.Property)} = {TranslateValue(a.Value)}"));

        var whereParts = new List<string>();
        if (upd.Where is not null)
            whereParts.Add(TranslateCondition(upd.Where));
        AppendEntityFilters(upd.Alias, whereParts);

        if (upd.Joins.Count > 0)
        {
            _sb.Append(FormatBulkUpdate(entity, upd.Alias, setClause, whereParts));
        }
        else
        {
            _sb.Append("UPDATE ");
            _sb.Append(FormatTable(entity));
            _sb.Append(' ').Append(upd.Alias);
            _sb.Append(" SET ").Append(setClause);
            if (whereParts.Count > 0)
                _sb.Append(" WHERE ").Append(string.Join(" AND ", whereParts));
        }

        return _sb.ToString();
    }

    private string FormatBulkUpdate(EntityModel entity, string alias, string setClause, List<string> whereParts)
    {
        var table = FormatTable(entity);
        var joinSql = new StringBuilder();
        foreach (var join in _ctx.ExplicitJoins)
        {
            var source = _ctx.GetAliasEntity(join.SourceAlias)!;
            var rel = source.Relationships.First(r => r.PropertyName == join.RelationshipProperty);
            var target = ResolveTargetEntity(rel)!;
            var joinType = join.IsLeft ? "LEFT JOIN" : "INNER JOIN";
            joinSql.Append(' ').Append(joinType).Append(' ')
                .Append(FormatTable(target)).Append(' ').Append(join.JoinAlias)
                .Append(" ON ").Append(FormatJoinOn(source, join.SourceAlias, rel, join.JoinAlias, target));
        }

        var where = whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : string.Empty;

        return _ctx.Provider switch
        {
            "PostgreSql" => $"UPDATE {table} {alias} SET {setClause} FROM {table} {alias}{joinSql}{where}",
            "MySql" => $"UPDATE {table} {alias}{joinSql} SET {setClause}{where}",
            _ => $"UPDATE {alias} SET {setClause} FROM {table} {alias}{joinSql}{where}",
        };
    }

    private string TranslateDelete(CpqlDeleteStatementNode del)
    {
        var entity = _ctx.ResolveEntityByName(del.EntityName)
            ?? throw new InvalidOperationException("Unknown entity " + del.EntityName);
        _ctx.Aliases[del.Alias] = entity;

        if (entity.SoftDeleteColumn is not null)
        {
            _sb.Clear();
            _sb.Append("UPDATE ").Append(FormatTable(entity)).Append(' ').Append(del.Alias);
            _sb.Append(" SET ").Append(SoftDeleteGenerator.BuildSoftDeleteSetClause(entity, _ctx.Provider, del.Alias + "."));
            var whereParts = new List<string>();
            if (del.Where is not null)
                whereParts.Add(TranslateCondition(del.Where));
            AppendEntityFilters(del.Alias, whereParts, includeSoftDelete: false);
            if (whereParts.Count > 0)
                _sb.Append(" WHERE ").Append(string.Join(" AND ", whereParts));
            return _sb.ToString();
        }

        _sb.Clear();
        _sb.Append("DELETE FROM ").Append(FormatTable(entity)).Append(' ').Append(del.Alias);
        var parts = new List<string>();
        if (del.Where is not null)
            parts.Add(TranslateCondition(del.Where));
        AppendEntityFilters(del.Alias, parts);
        if (parts.Count > 0)
            _sb.Append(" WHERE ").Append(string.Join(" AND ", parts));
        return _sb.ToString();
    }

    private void RegisterFrom(CpqlFromNode from)
    {
        if (_ctx.CteNames.ContainsKey(from.EntityOrCteName))
        {
            from.IsCte = true;
            _ctx.Aliases[from.Alias] = _ctx.RootEntity;
            return;
        }
        var entity = _ctx.ResolveEntityByName(from.EntityOrCteName)
            ?? _ctx.RootEntity;
        _ctx.Aliases[from.Alias] = entity;
    }

    private void RegisterExplicitJoin(CpqlJoinNode join)
    {
        var source = _ctx.GetAliasEntity(join.SourceAlias)
            ?? throw new InvalidOperationException("Unknown alias " + join.SourceAlias);
        var rel = source.Relationships.FirstOrDefault(r => r.PropertyName == join.RelationshipProperty)
            ?? throw new InvalidOperationException("Unknown relationship " + join.RelationshipProperty);
        var target = ResolveTargetEntity(rel) ?? throw new InvalidOperationException("Unknown target for " + rel.PropertyName);
        _ctx.Aliases[join.JoinAlias] = target;
        _ctx.ExplicitJoins.Add(join);
    }

    private void CollectImplicitJoins(CpqlSelectStatementNode sel)
    {
        foreach (var item in sel.SelectList.Items)
            WalkValueForPaths(item.Value);
        if (sel.Where is not null) WalkConditionForPaths(sel.Where);
        foreach (var g in sel.GroupBy) WalkValueForPaths(g);
        if (sel.Having is not null) WalkConditionForPaths(sel.Having);
        foreach (var o in sel.OrderBy) WalkValueForPaths(o.Value);
    }

    private void WalkConditionForPaths(CpqlConditionNode cond)
    {
        switch (cond)
        {
            case CpqlAndNode a:
                WalkConditionForPaths(a.Left);
                WalkConditionForPaths(a.Right);
                break;
            case CpqlOrNode o:
                WalkConditionForPaths(o.Left);
                WalkConditionForPaths(o.Right);
                break;
            case CpqlNotNode n:
                WalkConditionForPaths(n.Inner);
                break;
            case CpqlPredicateNode p:
                if (p.Left is not null) WalkValueForPaths(p.Left);
                if (p.Right is not null) WalkValueForPaths(p.Right);
                if (p.Right2 is not null) WalkValueForPaths(p.Right2);
                if (p.Subquery is not null) WalkSubquery(p.Subquery.Select);
                break;
        }
    }

    private void WalkSubquery(CpqlSelectStatementNode sel)
    {
        foreach (var item in sel.SelectList.Items)
            WalkValueForPaths(item.Value);
        if (sel.Where is not null) WalkConditionForPaths(sel.Where);
    }

    private void WalkValueForPaths(CpqlValueNode node)
    {
        if (node is CpqlPropertyPathNode path && path.Path.Count > 0)
            EnsureImplicitJoins(path);
        else if (node is CpqlArithmeticNode ar)
        {
            WalkValueForPaths(ar.Left);
            WalkValueForPaths(ar.Right);
        }
        else if (node is CpqlCaseNode c)
        {
            if (c.Input is not null) WalkValueForPaths(c.Input);
            foreach (var w in c.Whens) WalkConditionForPaths(w.Condition);
            if (c.Else is not null) WalkValueForPaths(c.Else);
        }
        else if (node is CpqlNewExprNode n)
        {
            foreach (var a in n.Arguments) WalkValueForPaths(a);
        }
        else if (node is CpqlAggregateNode ag && ag.Argument is not null)
            WalkValueForPaths(ag.Argument);
        else if (node is CpqlScalarFunctionNode sf)
        {
            foreach (var a in sf.Arguments) WalkValueForPaths(a);
        }
        else if (node is CpqlWindowNode w)
        {
            WalkValueForPaths(w.Inner);
            foreach (var p in w.PartitionBy) WalkValueForPaths(p);
            foreach (var o in w.OrderBy) WalkValueForPaths(o.Value);
        }
        else if (node is CpqlSubqueryNode sq)
            WalkSubquery(sq.Select);
    }

    private void EnsureImplicitJoins(CpqlPropertyPathNode path)
    {
        var alias = path.Alias;
        var entity = _ctx.GetAliasEntity(alias);
        if (entity is null || path.Path.Count == 0)
            return;

        var segments = path.Path;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            var relName = segments[i];
            var rel = entity.Relationships.FirstOrDefault(r => r.PropertyName == relName)
                ?? throw new InvalidOperationException($"Relationship '{relName}' not found on {entity.ClassName}");
            var target = ResolveTargetEntity(rel)
                ?? throw new InvalidOperationException($"Target entity not found for relationship '{relName}' on {entity.ClassName}");
            var joinAlias = $"j_{alias}_{string.Join("_", segments.Take(i + 1))}";
            _ctx.RegisterImplicitJoin(alias, relName, joinAlias);
            entity = target;
            alias = joinAlias;
            _ctx.Aliases[joinAlias] = entity;
        }
    }

    private void EmitAllJoins()
    {
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var join in _ctx.ExplicitJoins.Concat(_ctx.ImplicitJoins))
        {
            var key = join.SourceAlias + "." + join.RelationshipProperty;
            if (!emitted.Add(key)) continue;
            var source = _ctx.GetAliasEntity(join.SourceAlias)!;
            var rel = source.Relationships.First(r => r.PropertyName == join.RelationshipProperty);
            var target = ResolveTargetEntity(rel)!;
            _ctx.Aliases[join.JoinAlias] = target;
            var joinType = join.IsLeft ? "LEFT JOIN" : "INNER JOIN";
            _sb.Append(' ').Append(joinType).Append(' ');
            _sb.Append(FormatTable(target)).Append(' ').Append(join.JoinAlias);
            _sb.Append(" ON ").Append(FormatJoinOn(source, join.SourceAlias, rel, join.JoinAlias, target));
        }
    }

    private string FormatJoinOn(EntityModel source, string sourceAlias, RelationshipModel rel, string joinAlias, EntityModel target)
    {
        var fk = rel.ForeignKeyColumn ?? "id";
        var targetId = target.Properties.FirstOrDefault(p => p.IsId)?.ColumnName ?? "id";
        return $"{sourceAlias}.{fk} = {joinAlias}.{targetId}";
    }

    private string TranslateValue(CpqlValueNode node)
    {
        switch (node)
        {
            case CpqlPropertyPathNode path:
                return TranslatePropertyPath(path);
            case CpqlParameterNode p:
                _ctx.Parameters.Add(p.Name);
                return "@" + p.Name;
            case CpqlLiteralNode lit:
                if (lit.IsNull) return "NULL";
                if (lit.IsTrue) return CpqlScalarFunctions.EmitBooleanLiteral(true, _ctx.Provider);
                if (lit.IsFalse) return CpqlScalarFunctions.EmitBooleanLiteral(false, _ctx.Provider);
                if (lit.Value is string s) return "'" + s.Replace("'", "''") + "'";
                return lit.Value?.ToString() ?? "NULL";
            case CpqlArithmeticNode ar:
                return $"({TranslateValue(ar.Left)} {ar.Op} {TranslateValue(ar.Right)})";
            case CpqlCaseNode c:
                return TranslateCase(c);
            case CpqlNewExprNode n:
                return string.Join(", ", n.Arguments.Select(TranslateValue));
            case CpqlAggregateNode ag:
                return TranslateAggregate(ag);
            case CpqlScalarFunctionNode sf:
                var args = sf.Arguments.Select(TranslateValue).ToList();
                return CpqlScalarFunctions.Emit(sf.Name, args, _ctx.Provider, sf.CastType);
            case CpqlWindowNode win:
                return TranslateWindow(win);
            case CpqlSubqueryNode sq:
                return "(" + TranslateSelect(sq.Select, null) + ")";
            default:
                throw new InvalidOperationException("Unknown value node");
        }
    }

    private string TranslatePropertyPath(CpqlPropertyPathNode path)
    {
        if (path.Path.Count == 0)
        {
            var ent = _ctx.GetAliasEntity(path.Alias)!;
            return string.Join(", ", ent.Properties.Select(p => $"{path.Alias}.{p.ColumnName}"));
        }
        EnsureImplicitJoins(path);
        var alias = path.Alias;
        var entity = _ctx.GetAliasEntity(alias)!;
        var propName = path.Path[path.Path.Count - 1];
        if (path.Path.Count > 1)
        {
            alias = $"j_{path.Alias}_{string.Join("_", path.Path.Take(path.Path.Count - 1))}";
            entity = _ctx.GetAliasEntity(alias)!;
        }
        return $"{alias}.{ResolveColumnOnEntity(entity, propName)}";
    }

    private string ResolveColumn(string alias, string property)
    {
        var entity = _ctx.GetAliasEntity(alias)!;
        return ResolveColumnOnEntity(entity, property);
    }

    private static string ResolveColumnOnEntity(EntityModel entity, string property)
    {
        var prop = entity.Properties.FirstOrDefault(p => p.PropertyName == property)
            ?? throw new InvalidOperationException($"Property '{property}' not found on {entity.ClassName}");
        return prop.ColumnName;
    }

    private string TranslateAggregate(CpqlAggregateNode ag)
    {
        if (ag.CountStar) return "COUNT(*)";
        var fn = ag.Function.ToUpperInvariant();
        var arg = ag.Argument is null ? "*"
            : ag.Argument is CpqlPropertyPathNode { Path.Count: 0 }
                ? "*"
                : TranslateValue(ag.Argument);
        if (ag.Distinct) return $"{fn}(DISTINCT {arg})";
        return $"{fn}({arg})";
    }

    private string TranslateWindow(CpqlWindowNode win)
    {
        var inner = TranslateValue(win.Inner);
        _sb.Length = 0;
        var over = new StringBuilder();
        over.Append(inner).Append(" OVER (");
        if (win.PartitionBy.Count > 0)
            over.Append("PARTITION BY ").Append(string.Join(", ", win.PartitionBy.Select(TranslateValue)));
        if (win.OrderBy.Count > 0)
        {
            if (win.PartitionBy.Count > 0) over.Append(' ');
            over.Append("ORDER BY ").Append(string.Join(", ", win.OrderBy.Select(TranslateOrderItem)));
        }
        if (!string.IsNullOrEmpty(win.FrameSpec))
        {
            if (win.PartitionBy.Count > 0 || win.OrderBy.Count > 0) over.Append(' ');
            over.Append(win.FrameSpec);
        }
        over.Append(')');
        return over.ToString();
    }

    private string TranslateCase(CpqlCaseNode c)
    {
        var sb = new StringBuilder("CASE ");
        if (c.Input is not null)
            sb.Append(TranslateValue(c.Input)).Append(' ');
        foreach (var w in c.Whens)
            sb.Append("WHEN ").Append(TranslateCondition(w.Condition))
                .Append(" THEN ").Append(TranslateValue(w.Result)).Append(' ');
        sb.Append("ELSE ").Append(TranslateValue(c.Else!)).Append(" END");
        return sb.ToString();
    }

    private string TranslateCondition(CpqlConditionNode cond)
    {
        switch (cond)
        {
            case CpqlAndNode a:
                return $"({TranslateCondition(a.Left)} AND {TranslateCondition(a.Right)})";
            case CpqlOrNode o:
                return $"({TranslateCondition(o.Left)} OR {TranslateCondition(o.Right)})";
            case CpqlNotNode n:
                return $"(NOT {TranslateCondition(n.Inner)})";
            case CpqlPredicateNode p:
                return TranslatePredicate(p);
            default:
                throw new InvalidOperationException("Unknown condition");
        }
    }

    private string TranslatePredicate(CpqlPredicateNode p)
    {
        switch (p.Kind)
        {
            case CpqlPredicateKind.Comparison:
                return $"{TranslateValue(p.Left!)} {p.ComparisonOp} {TranslateValue(p.Right!)}";
            case CpqlPredicateKind.Between:
                return $"{TranslateValue(p.Left!)} BETWEEN {TranslateValue(p.Right!)} AND {TranslateValue(p.Right2!)}";
            case CpqlPredicateKind.InParameter:
                _ctx.Parameters.Add(p.ParameterName!);
                return $"{TranslateValue(p.Left!)} IN @{p.ParameterName}";
            case CpqlPredicateKind.InSubquery:
                return $"{TranslateValue(p.Left!)} IN ({TranslateSelect(p.Subquery!.Select, null)})";
            case CpqlPredicateKind.Like:
                return $"{TranslateValue(p.Left!)} LIKE {TranslateValue(p.Right!)}";
            case CpqlPredicateKind.IsNull:
                var expr = TranslateValue(p.Left!);
                return p.Negated ? $"{expr} IS NOT NULL" : $"{expr} IS NULL";
            case CpqlPredicateKind.IsTrue:
                return $"{TranslateValue(p.Left!)} = {CpqlScalarFunctions.EmitBooleanLiteral(true, _ctx.Provider)}";
            case CpqlPredicateKind.IsFalse:
                return $"{TranslateValue(p.Left!)} = {CpqlScalarFunctions.EmitBooleanLiteral(false, _ctx.Provider)}";
            case CpqlPredicateKind.Exists:
                return $"EXISTS ({TranslateSelect(p.Subquery!.Select, null)})";
            default:
                throw new InvalidOperationException("Unknown predicate");
        }
    }

    private string TranslateOrderItem(CpqlOrderItemNode item)
    {
        var s = TranslateValue(item.Value);
        s += item.Ascending ? " ASC" : " DESC";
        if (item.NullsFirst == true) s += " NULLS FIRST";
        else if (item.NullsFirst == false) s += " NULLS LAST";
        return s;
    }

    private void AppendEntityFilters(string alias, List<string> parts, bool includeSoftDelete = true)
    {
        var entity = _ctx.GetAliasEntity(alias);
        if (entity is null) return;
        if (_ctx.ApplySoftDeleteFilter && includeSoftDelete && entity.SoftDeleteColumn is not null)
            parts.Add($"{alias}.{entity.SoftDeleteColumn} = 0");
        if (entity.TenantIdColumn is not null)
            parts.Add($"{alias}.{entity.TenantIdColumn} = @tenantId");
    }

    private static string FormatTable(EntityModel entity)
        => entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;

    private string FormatTableRef(CpqlFromNode from)
    {
        if (from.IsCte)
            return $"{from.EntityOrCteName} {from.Alias}";
        var entity = _ctx.GetAliasEntity(from.Alias)!;
        return $"{FormatTable(entity)} {from.Alias}";
    }

    private EntityModel? ResolveTargetEntity(RelationshipModel rel)
    {
        var fqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(fqn)) return null;
        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (_ctx.AllModels.TryGetValue(key, out var model))
            return model;
        foreach (var m in _ctx.AllModels.Values)
        {
            if (string.Equals(m.FullyQualifiedName, key, StringComparison.Ordinal)
                || string.Equals(m.ClassName, key, StringComparison.Ordinal))
                return m;
        }
        return null;
    }
}
