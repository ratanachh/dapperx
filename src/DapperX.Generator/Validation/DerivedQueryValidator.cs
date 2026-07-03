using DapperX.Generator.MethodNameParsing;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.MethodNameParsing;
using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class DerivedQueryValidator
{
    public static bool ValidateParsedQuery(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        string provider,
        SourceProductionContext ctx)
    {
        if (parsed.Subject is SubjectKind.Insert or SubjectKind.Update)
            return ValidateWriteOperation(method, entity, parsed, ctx);

        if (!ValidatePropertyPaths(method, entity, parsed, ctx))
            return false;

        return ValidateRegexOperator(method, entity, parsed, provider, ctx);
    }

    public static bool ValidateWriteOperation(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        SourceProductionContext ctx)
    {
        var valid = true;

        if (parsed.Conditions.Any() || parsed.OrderBySegments.Any())
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WriteOperationWithConditions,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
            valid = false;
        }

        if (FindEntityParameter(method, entity) is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WriteOperationInvalidSignature,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName,
                entity.FullyQualifiedName));
            valid = false;
        }

        return valid;
    }

    public static bool ValidateBulkOperation(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        SourceProductionContext ctx)
    {
        var valid = true;

        if (parsed.Conditions.Any() || parsed.OrderBySegments.Any())
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.WriteOperationWithConditions,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
            valid = false;
        }

        if (parsed.Subject is not (SubjectKind.Insert or SubjectKind.Update or SubjectKind.Delete))
            valid = false;

        if (FindEntityCollectionParameter(method, entity) is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BulkOperationInvalidSignature,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName,
                entity.ClassName));
            valid = false;
        }

        return valid;
    }

    public static IParameterSymbol? FindEntityParameter(IMethodSymbol method, EntityModel entity)
    {
        var entityParams = method.Parameters
            .Where(p => !IsInfrastructureParameter(p))
            .Where(p => IsEntityType(p.Type, entity.FullyQualifiedName))
            .ToList();
        return entityParams.Count == 1 ? entityParams[0] : null;
    }

    public static IParameterSymbol? FindEntityCollectionParameter(IMethodSymbol method, EntityModel entity)
    {
        return method.Parameters
            .Where(p => !IsInfrastructureParameter(p))
            .FirstOrDefault(p => IsEntityEnumerable(p.Type, entity.FullyQualifiedName));
    }

    private static bool ValidatePropertyPaths(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        SourceProductionContext ctx)
    {
        var propertyNames = new HashSet<string>(
            entity.DerivedQueryPaths.Select(p => p.PathKey),
            StringComparer.Ordinal);

        var valid = true;

        foreach (var condition in parsed.Conditions)
        {
            if (propertyNames.Contains(condition.PropertyName))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UnknownDerivedQueryProperty,
                method.Locations.FirstOrDefault(),
                condition.PropertyName,
                method.Name,
                entity.ClassName));
            valid = false;
        }

        foreach (var segment in parsed.OrderBySegments)
        {
            if (propertyNames.Contains(segment.PropertyName))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.UnknownDerivedQueryProperty,
                method.Locations.FirstOrDefault(),
                segment.PropertyName,
                method.Name,
                entity.ClassName));
            valid = false;
        }

        return valid;
    }

    private static bool ValidateRegexOperator(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        string provider,
        SourceProductionContext ctx)
    {
        if (!parsed.Conditions.Any(c => c.Operator == OperatorKind.Regex))
            return true;

        if (provider == "SqlServer")
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.RegexNotSupportedOnProvider,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName,
                provider));
            return false;
        }

        if (provider == "Sqlite")
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.RegexWarningOnSqlite,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
        }

        return true;
    }

    public static bool ValidateEntityGraphUsage(
        IMethodSymbol method,
        EntityModel entity,
        ParsedDerivedQuery parsed,
        SourceProductionContext ctx)
    {
        if (!HasEntityGraphParameter(method))
            return true;

        var valid = true;

        if (entity.NamedEntityGraphs.Count == 0)
            return true;

        if (parsed.Subject is not (SubjectKind.Find or SubjectKind.Stream))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.EntityGraphWithInclude,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
            valid = false;
        }

        var pathKeys = parsed.Conditions.Select(c => c.PropertyName)
            .Concat(parsed.OrderBySegments.Select(s => s.PropertyName));
        var hasNavigationJoin = pathKeys
            .Select(k => entity.DerivedQueryPaths.FirstOrDefault(p => p.PathKey == k))
            .Any(p => p?.Kind == DerivedQueryPathKind.NavigationJoin);

        if (hasNavigationJoin)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.EntityGraphWithInclude,
                method.Locations.FirstOrDefault(),
                method.Name,
                entity.ClassName));
            valid = false;
        }

        return valid;
    }

    public static bool HasEntityGraphParameter(IMethodSymbol method)
        => method.Parameters.Any(p =>
            p.Name.Equals("entityGraph", StringComparison.OrdinalIgnoreCase)
            || p.Name.Equals("EntityGraph", StringComparison.OrdinalIgnoreCase));

    public static void ValidateIncludeDeletedParameter(
        IMethodSymbol method,
        EntityModel entity,
        SourceProductionContext ctx)
    {
        if (!method.Parameters.Any(p => p.Name == "includeDeleted"))
            return;

        if (entity.SoftDeleteColumn is not null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.IncludeDeletedWithoutSoftDelete,
            method.Locations.FirstOrDefault(),
            method.Name,
            entity.ClassName));
    }

    private static bool IsInfrastructureParameter(IParameterSymbol p)
        => p.Name is "transaction" or "ct";

    private static bool IsEntityType(ITypeSymbol type, string entityFqn)
        => NormalizeFqn(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
           == NormalizeFqn(entityFqn);

    private static bool IsEntityEnumerable(ITypeSymbol type, string entityFqn)
    {
        if (type is not INamedTypeSymbol named || !named.IsGenericType)
            return false;

        if (named.TypeArguments.Length != 1)
            return false;

        var defName = named.OriginalDefinition.Name;
        if (defName is not "IEnumerable" and not "List" and not "ICollection")
            return false;

        return IsEntityType(named.TypeArguments[0], entityFqn);
    }

    private static string NormalizeFqn(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal)
            ? fqn.Substring("global::".Length)
            : fqn;
}
