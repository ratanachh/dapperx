namespace DapperX.Generator.Validation;

using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class GeneratedColumnValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        foreach (var prop in entity.Properties.Where(p => p.GeneratedTime is not null))
        {
            if (prop.GeneratedTime is not ("Insert" or "Always"))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidGeneratedGenerationTime,
                    symbol.Locations.FirstOrDefault(),
                    prop.PropertyName,
                    entity.ClassName));
            }
        }
    }
}
