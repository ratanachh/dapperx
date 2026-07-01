namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

internal static class TenancyValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (entity.TenantIdColumn is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.MissingTenantProvider,
            symbol.Locations.FirstOrDefault(),
            entity.ClassName));
    }
}
