using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class FormulaValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        foreach (var prop in entity.Properties.Where(p => p.Formula is not null))
        {
            var location = symbol.GetMembers(prop.PropertyName).OfType<IPropertySymbol>().FirstOrDefault()?.Locations.FirstOrDefault()
                ?? symbol.Locations.FirstOrDefault();

            if (prop.IsId)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.FormulaOnId, location, prop.PropertyName, entity.ClassName));
            }

            if (prop.IsVersion)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.FormulaOnVersion, location, prop.PropertyName, entity.ClassName));
            }

            if (prop.IsSortable)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.FormulaOnSortable, location, prop.PropertyName, entity.ClassName));
            }
        }
    }
}
