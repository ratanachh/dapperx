using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class CompositeKeyValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        if (!entity.HasCompositeKey || entity.CompositeKey is null)
            return;

        if (SyntaxHelper.HasAttribute(symbol, SyntaxHelper.EmbeddedIdAttr))
        {
            var embeddedIdProp = symbol.GetMembers().OfType<IPropertySymbol>()
                .FirstOrDefault(p => SyntaxHelper.HasAttribute(p, SyntaxHelper.EmbeddedIdAttr));
            if (embeddedIdProp?.Type is INamedTypeSymbol embedType
                && !SyntaxHelper.HasAttribute(embedType, SyntaxHelper.EmbeddableAttr))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CompositeKeyNotEmbeddable,
                    embeddedIdProp.Locations.FirstOrDefault(),
                    embeddedIdProp.Name,
                    entity.ClassName,
                    embedType.Name));
            }
        }

        foreach (var part in entity.CompositeKey.Parts)
        {
            if (part.IdGenerationStrategy is not null && part.IdGenerationStrategy != "Assigned")
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CompositeKeyGeneratedValue,
                    symbol.Locations.FirstOrDefault(),
                    part.KeyClassPropertyName,
                    entity.ClassName));
            }
        }
    }
}
