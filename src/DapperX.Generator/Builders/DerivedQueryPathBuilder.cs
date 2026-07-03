using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class DerivedQueryPathBuilder
{
    private const string MainAlias = "e";

    public static IReadOnlyList<DerivedQueryPathModel> Build(
        EntityModel entity,
        INamedTypeSymbol entitySymbol,
        IReadOnlyList<PropertyModel> properties,
        IReadOnlyList<RelationshipModel> relationships)
    {
        var paths = new List<DerivedQueryPathModel>();

        foreach (var prop in properties.Where(p => p.Formula is null))
        {
            paths.Add(new DerivedQueryPathModel
            {
                PathKey = prop.PropertyName,
                Kind = DerivedQueryPathKind.Direct,
                ColumnExpression = prop.ColumnName,
                IsSortable = prop.IsSortable,
            });
        }

        foreach (var member in entitySymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                continue;

            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.EmbeddedAttr))
                AddEmbeddedPaths(member, paths);

            var rel = relationships.FirstOrDefault(r => r.PropertyName == member.Name);
            if (rel is null)
                continue;

            if (rel.Kind is not ("ManyToOne" or "OneToOne"))
                continue;

            if (rel.IsPrimaryKeyJoin || string.IsNullOrEmpty(rel.ForeignKeyColumn))
                continue;

            AddNavigationPaths(member, rel, paths);
        }

        return paths
            .GroupBy(p => p.PathKey, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderByDescending(p => p.PathKey.Length)
            .ToList();
    }

    private static void AddEmbeddedPaths(IPropertySymbol embeddedProp, List<DerivedQueryPathModel> paths)
    {
        if (embeddedProp.Type is not INamedTypeSymbol embedType)
            return;

        var prefix = embeddedProp.Name;
        var overrides = embeddedProp.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.AttributeOverrideAttr)
            .Select(a => (
                Property: SyntaxHelper.GetConstructorArg<string>(a, 0) ?? string.Empty,
                Column: SyntaxHelper.GetConstructorArg<string>(a, 1) ?? string.Empty))
            .Where(x => !string.IsNullOrEmpty(x.Property))
            .ToDictionary(x => x.Property, x => x.Column, StringComparer.Ordinal);

        foreach (var inner in embedType.GetMembers().OfType<IPropertySymbol>())
        {
            if (inner.IsStatic || inner.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.TransientAttr))
                continue;

            var pathKey = prefix + inner.Name;
            var columnName = overrides.TryGetValue(inner.Name, out var overridden)
                ? overridden
                : SyntaxHelper.ToSnakeCase(prefix) + "_" + SyntaxHelper.ToSnakeCase(inner.Name);

            paths.Add(new DerivedQueryPathModel
            {
                PathKey = pathKey,
                Kind = DerivedQueryPathKind.Embedded,
                ColumnExpression = columnName,
                IsSortable = SyntaxHelper.HasAttribute(inner, SyntaxHelper.SortableAttr),
            });
        }
    }

    private static void AddNavigationPaths(
        IPropertySymbol navProp,
        RelationshipModel rel,
        List<DerivedQueryPathModel> paths)
    {
        if (navProp.Type is not INamedTypeSymbol targetType)
            return;

        var fkColumn = rel.ForeignKeyColumn!;
        var joinAlias = $"nav_{rel.PropertyName}";
        var targetTable = ResolveTableName(targetType);
        var targetIdColumn = ResolveIdColumnName(targetType);
        var joinOn = $"{MainAlias}.{fkColumn} = {joinAlias}.{targetIdColumn}";

        paths.Add(new DerivedQueryPathModel
        {
            PathKey = rel.PropertyName + "Id",
            Kind = DerivedQueryPathKind.NavigationForeignKey,
            ColumnExpression = fkColumn,
            IsSortable = false,
        });

        foreach (var inner in targetType.GetMembers().OfType<IPropertySymbol>())
        {
            if (inner.IsStatic || inner.DeclaredAccessibility != Accessibility.Public)
                continue;
            if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.TransientAttr))
                continue;
            if (SyntaxHelper.HasAttribute(inner, SyntaxHelper.IdAttr))
                continue;

            var colAttr = SyntaxHelper.GetAttribute(inner, SyntaxHelper.ColumnAttr);
            var columnName = SyntaxHelper.GetNamedArg<string>(colAttr, "Name")
                               ?? SyntaxHelper.ToSnakeCase(inner.Name);
            var pathKey = rel.PropertyName + inner.Name;

            paths.Add(new DerivedQueryPathModel
            {
                PathKey = pathKey,
                Kind = DerivedQueryPathKind.NavigationJoin,
                ColumnExpression = $"{joinAlias}.{columnName}",
                JoinAlias = joinAlias,
                JoinTable = targetTable,
                JoinOnSql = joinOn,
                IsSortable = SyntaxHelper.HasAttribute(inner, SyntaxHelper.SortableAttr),
            });
        }
    }

    private static string ResolveTableName(INamedTypeSymbol type)
    {
        var tableAttr = SyntaxHelper.GetAttribute(type, SyntaxHelper.TableAttr);
        return SyntaxHelper.GetConstructorArg<string>(tableAttr, 0)
               ?? SyntaxHelper.ToSnakeCase(type.Name);
    }

    private static string ResolveIdColumnName(INamedTypeSymbol type)
    {
        var idProp = type.GetMembers().OfType<IPropertySymbol>()
            .FirstOrDefault(p => SyntaxHelper.HasAttribute(p, SyntaxHelper.IdAttr));
        if (idProp is null)
            return "id";
        var colAttr = SyntaxHelper.GetAttribute(idProp, SyntaxHelper.ColumnAttr);
        return SyntaxHelper.GetNamedArg<string>(colAttr, "Name")
               ?? SyntaxHelper.ToSnakeCase(idProp.Name);
    }

    public static string BuildSelectWithJoins(EntityModel entity, IEnumerable<DerivedQueryPathModel> requiredJoins)
    {
        var joinPaths = requiredJoins
            .Where(p => p.Kind == DerivedQueryPathKind.NavigationJoin && p.JoinTable is not null)
            .GroupBy(p => p.JoinAlias)
            .Select(g => g.First())
            .ToList();

        var cols = string.Join(", ", entity.Properties.Select(p => $"{MainAlias}.{p.ColumnName}"));
        var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
        var sql = $"SELECT {cols} FROM {table} {MainAlias}";

        foreach (var join in joinPaths)
            sql += $" INNER JOIN {join.JoinTable} {join.JoinAlias} ON {join.JoinOnSql}";

        return sql;
    }

    /// <summary>Qualify bare primary-table columns when a navigation JOIN is present.</summary>
    public static string QualifyColumn(DerivedQueryPathModel? path, string fallbackColumn, bool hasJoin)
    {
        var col = path?.ColumnExpression ?? fallbackColumn;
        if (!hasJoin || path?.Kind == DerivedQueryPathKind.NavigationJoin)
            return col;
        return $"{MainAlias}.{col}";
    }
}
