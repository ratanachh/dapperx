namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;

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
