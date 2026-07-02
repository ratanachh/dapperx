using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using System.Linq;
using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class RelationshipValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, string provider)
    {
        foreach (var rel in entity.Relationships.Where(r => r.Kind == "OneToMany"))
        {
            if (rel.IsLazyCollection || rel.IsLazyMap)
                continue;

            if (rel.TargetEntity is not null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.BatchLoadInvalidCollectionType,
                    ResolvePropertyLocation(symbol, rel.PropertyName),
                    rel.PropertyName,
                    entity.ClassName));
            }
        }

        foreach (var rel in entity.Relationships.Where(r => r.Kind == "ManyToMany"))
        {
            var location = ResolvePropertyLocation(symbol, rel.PropertyName);

            if (string.IsNullOrEmpty(rel.JoinTable))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ManyToManyMissingJoinTable,
                    location,
                    rel.PropertyName,
                    entity.ClassName));
                continue;
            }

            if (string.IsNullOrEmpty(rel.JoinTableFk))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.JoinTableMissingJoinColumn,
                    location,
                    rel.PropertyName,
                    entity.ClassName));
            }

            if (string.IsNullOrEmpty(rel.JoinTableInverseFk))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.JoinTableMissingInverseJoinColumn,
                    location,
                    rel.PropertyName,
                    entity.ClassName));
            }
        }
    }

    private static Location ResolvePropertyLocation(INamedTypeSymbol entity, string propertyName)
        => entity.GetMembers(propertyName).OfType<IPropertySymbol>().FirstOrDefault()?.Locations.FirstOrDefault()
           ?? entity.Locations.FirstOrDefault();
}
