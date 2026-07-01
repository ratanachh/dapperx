namespace DapperX.Generator.Validation;

using System.Collections.Generic;
using DapperX.Generator.Models;
using DapperX.Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class AssociationOverrideValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (entity.AssociationOverrides.Count == 0)
            return;

        var relationshipNames = new HashSet<string>(CollectRelationshipNames(symbol), StringComparer.Ordinal);
        foreach (var rel in entity.Relationships)
            relationshipNames.Add(rel.PropertyName);

        foreach (var ov in entity.AssociationOverrides)
        {
            if (relationshipNames.Contains(ov.RelationshipPropertyName))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.AssociationOverrideNotFound,
                symbol.Locations.FirstOrDefault(),
                entity.ClassName,
                ov.RelationshipPropertyName));
        }
    }

    private static IEnumerable<string> CollectRelationshipNames(INamedTypeSymbol type)
    {
        if (type.BaseType is not null
            && SyntaxHelper.HasAttribute(type.BaseType, SyntaxHelper.MappedSuperclassAttr))
        {
            foreach (var name in CollectRelationshipNames(type.BaseType))
                yield return name;
        }

        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.OneToManyAttr)
                || SyntaxHelper.HasAttribute(member, SyntaxHelper.ManyToOneAttr)
                || SyntaxHelper.HasAttribute(member, SyntaxHelper.OneToOneAttr)
                || SyntaxHelper.HasAttribute(member, SyntaxHelper.ManyToManyAttr))
                yield return member.Name;
        }
    }
}
