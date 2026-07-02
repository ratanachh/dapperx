using Dapper.Npa.Generator.Emitters;
using Dapper.Npa.Generator.Models;

namespace Dapper.Npa.Generator.Utils;

using Generator.Builders;
using Generator.Emitters;
using Generator.Models;
using Microsoft.CodeAnalysis;

internal static class ProjectionCollector
{
    public static IReadOnlyList<ProjectionModel> Collect(
        Compilation compilation,
        EntityModel entity,
        SourceProductionContext? ctx = null,
        Location? location = null)
    {
        var results = new List<ProjectionModel>();
        var entityMetadataName = ToMetadataName(entity.FullyQualifiedName);

        foreach (var type in EnumerateNamedTypes(compilation.Assembly.GlobalNamespace))
        {
            if (!SyntaxHelper.HasAttribute(type, SyntaxHelper.ProjectionAttr))
                continue;

            var attr = type.GetAttributes()
                .First(a => a.AttributeClass?.ToDisplayString() == SyntaxHelper.ProjectionAttr);
            if (attr.ConstructorArguments.Length == 0
                || attr.ConstructorArguments[0].Value is not INamedTypeSymbol fromType)
                continue;

            var fromMetadata = ToMetadataName(fromType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            if (!string.Equals(fromMetadata, entityMetadataName, StringComparison.Ordinal))
                continue;

            var baseSql = TryBuildProjectionSql(entity, type, ctx, location);
            if (baseSql is null)
                continue;

            results.Add(new ProjectionModel
            {
                DtoMetadataName = ToMetadataName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                DtoDisplayName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                BaseSelectSql = baseSql,
            });
        }

        return results;
    }

    private static string? TryBuildProjectionSql(
        EntityModel entity,
        INamedTypeSymbol dtoType,
        SourceProductionContext? ctx,
        Location? location)
    {
        const string mainAlias = "e";
        var cols = new List<string>();
        foreach (var prop in dtoType.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.IsStatic || prop.IsIndexer)
                continue;
            if (prop.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                continue;

            var entityProp = entity.Properties.FirstOrDefault(p =>
                string.Equals(p.PropertyName, prop.Name, StringComparison.Ordinal));
            if (entityProp is null)
            {
                ctx?.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ProjectionPropertyNotMapped,
                    location,
                    prop.Name,
                    dtoType.Name,
                    entity.ClassName));
                return null;
            }

            if (entityProp.IsTransient)
            {
                ctx?.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ProjectionPropertyNotMapped,
                    location,
                    prop.Name,
                    dtoType.Name,
                    entity.ClassName));
                return null;
            }

            var expr = FormulaEmitter.FormatProjectionExpression(entityProp, mainAlias);
            cols.Add($"{expr} AS {entityProp.ColumnName}");
        }

        if (cols.Count == 0)
            return null;

        var table = entity.Schema is not null ? $"{entity.Schema}.{entity.TableName}" : entity.TableName;
        var joins = SecondaryJoins(entity);
        return $"SELECT {string.Join(", ", cols)} FROM {table} {mainAlias}{joins}";
    }

    private static string SecondaryJoins(EntityModel entity)
    {
        var secondary = entity.Properties
            .Where(p => p.SecondaryTable is not null)
            .GroupBy(p => p.SecondaryTable!)
            .ToList();
        if (secondary.Count == 0)
            return string.Empty;

        var idProp = entity.Properties.First(p => p.IsId);
        var sb = new System.Text.StringBuilder();
        foreach (var group in secondary)
        {
            var alias = $"st_{Sanitize(group.Key)}";
            sb.Append($" LEFT JOIN {group.Key} {alias} ON {alias}.{idProp.ColumnName} = e.{idProp.ColumnName}");
        }
        return sb.ToString();
    }

    private static string Sanitize(string tableName)
        => tableName.Replace('.', '_').Replace('[', '_').Replace(']', '_');

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (var nested in EnumerateNamedTypes(type))
                yield return nested;
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            foreach (var type in EnumerateNamedTypes(childNs))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var deep in EnumerateNamedTypes(nested))
                yield return deep;
        }
    }

    private static string ToMetadataName(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
}
