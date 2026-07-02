using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class ElementCollectionValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!SyntaxHelper.HasAttribute(member, SyntaxHelper.ElementCollectionAttr))
                continue;

            var hasCollectionTable = SyntaxHelper.HasAttribute(member, SyntaxHelper.CollectionTableAttr);
            if (!hasCollectionTable)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MissingCollectionTable,
                    member.Locations.FirstOrDefault(),
                    member.Name,
                    entity.ClassName));
            }
        }
    }
}
