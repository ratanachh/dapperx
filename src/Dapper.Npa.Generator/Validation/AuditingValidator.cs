using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class AuditingValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (entity.Auditing?.CreatedByProperty is null && entity.Auditing?.LastModifiedByProperty is null)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.MissingAuditingProvider,
            symbol.Locations.FirstOrDefault(),
            entity.ClassName));
    }
}
