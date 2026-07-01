namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class MapKeyValidator
{
    public static void Validate(
        EntityModel entity,
        INamedTypeSymbol symbol,
        SourceProductionContext ctx,
        IReadOnlyDictionary<string, EntityModel> allModels)
    {
        foreach (var rel in entity.Relationships.Where(r => r.Kind == "OneToMany" && r.IsLazyMap))
        {
            var propLocation = ResolvePropertyLocation(symbol, rel.PropertyName)
                ?? symbol.Locations.FirstOrDefault();

            if (string.IsNullOrEmpty(rel.MapKeyColumn))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MapKeyMissing,
                    propLocation,
                    rel.PropertyName,
                    entity.ClassName));
                continue;
            }

            if (string.IsNullOrEmpty(rel.TargetEntity))
                continue;

            if (!TryResolveChildModel(rel.TargetEntity, allModels, out var childModel))
                continue;

            if (string.IsNullOrEmpty(rel.MapKeyPropertyName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MapKeyColumnNotFound,
                    propLocation,
                    rel.MapKeyColumn,
                    childModel.ClassName,
                    rel.PropertyName,
                    entity.ClassName));
                continue;
            }

            var mapKeyProp = childModel.Properties.FirstOrDefault(p =>
                p.PropertyName == rel.MapKeyPropertyName);
            if (mapKeyProp is null)
                continue;

            if (!TypeCompatibilityHelper.AreCompatible(rel.MapKeyClrTypeName, mapKeyProp.ClrTypeName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MapKeyTypeMismatch,
                    propLocation,
                    rel.MapKeyClrTypeName ?? "?",
                    mapKeyProp.ClrTypeName,
                    rel.PropertyName,
                    entity.ClassName));
            }

            if (!TypeCompatibilityHelper.AreCompatible(rel.TargetEntity, childModel.FullyQualifiedName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MapKeyTypeMismatch,
                    propLocation,
                    rel.TargetEntity ?? "?",
                    childModel.FullyQualifiedName,
                    rel.PropertyName,
                    entity.ClassName));
            }
        }
    }

    private static bool TryResolveChildModel(
        string targetFqn,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel childModel)
    {
        var normalized = NormalizeFqn(targetFqn);
        if (allModels.TryGetValue(normalized, out childModel!))
            return true;

        return allModels.TryGetValue(targetFqn, out childModel!);
    }

    private static Location? ResolvePropertyLocation(INamedTypeSymbol entity, string propertyName)
        => entity.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault()?.Locations.FirstOrDefault();

    private static string NormalizeFqn(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;
}
