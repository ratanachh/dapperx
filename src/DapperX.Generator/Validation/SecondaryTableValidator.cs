using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class SecondaryTableValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, string provider)
    {
        _ = provider;

        foreach (var prop in entity.Properties)
        {
            if (prop.IsId && !string.IsNullOrEmpty(prop.SecondaryTable))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.IdOnSecondaryTable,
                    symbol.Locations.FirstOrDefault(),
                    prop.PropertyName,
                    entity.ClassName,
                    prop.SecondaryTable));
            }
        }

        foreach (var st in entity.SecondaryTables)
        {
            if (string.IsNullOrWhiteSpace(st.TableName))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MissingSecondaryTablePrimaryKeyJoinColumn,
                    symbol.Locations.FirstOrDefault(),
                    "<unnamed>",
                    entity.ClassName));
                continue;
            }

            if (string.IsNullOrWhiteSpace(st.PrimaryKeyJoinColumn))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MissingSecondaryTablePrimaryKeyJoinColumn,
                    symbol.Locations.FirstOrDefault(),
                    st.TableName,
                    entity.ClassName));
            }
        }

        foreach (var group in entity.SecondaryTables
                     .Where(st => !string.IsNullOrWhiteSpace(st.TableName))
                     .GroupBy(st => st.TableName, StringComparer.OrdinalIgnoreCase)
                     .Where(g => g.Count() > 1))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.DuplicateSecondaryTable,
                symbol.Locations.FirstOrDefault(),
                entity.ClassName,
                group.Key));
        }
    }
}
