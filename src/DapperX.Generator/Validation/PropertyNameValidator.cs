using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

/// <summary>
/// Warns when entity property names collide with core derived-query keywords (Requirements.md Section 5).
/// </summary>
internal static class PropertyNameValidator
{
    private static readonly HashSet<string> CoreReservedKeywords = new(StringComparer.Ordinal)
    {
        "And", "Or", "Not", "In", "Between", "Like",
        "True", "False", "Null",
        "Before", "After",
        "OrderBy", "First", "Top", "Distinct", "Count",
    };

    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, string provider)
    {
        foreach (var prop in entity.Properties)
        {
            if (!CoreReservedKeywords.Contains(prop.PropertyName))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ReservedPropertyName,
                symbol.Locations.FirstOrDefault(),
                prop.PropertyName,
                entity.ClassName,
                prop.PropertyName));
        }
    }
}
