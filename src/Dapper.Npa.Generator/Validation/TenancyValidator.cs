using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

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
