using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class EmbeddableValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        foreach (var member in symbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (!SyntaxHelper.HasAttribute(member, SyntaxHelper.EmbeddedAttr))
                continue;
            if (member.Type is not INamedTypeSymbol embedType)
                continue;
            ValidateEmbeddableType(embedType, ctx);
        }
    }

    public static void ValidateEmbeddableType(INamedTypeSymbol embedType, SourceProductionContext ctx)
    {
        if (!SyntaxHelper.HasAttribute(embedType, SyntaxHelper.EmbeddableAttr))
            return;

        if (SyntaxHelper.HasAttribute(embedType, SyntaxHelper.TableAttr))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.EmbeddableHasTable,
                embedType.Locations.FirstOrDefault(),
                embedType.Name));
        }

        foreach (var member in embedType.GetMembers().OfType<IPropertySymbol>())
        {
            if (SyntaxHelper.HasAttribute(member, SyntaxHelper.IdAttr))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.EmbeddableHasId,
                    member.Locations.FirstOrDefault(),
                    embedType.Name,
                    member.Name));
            }
        }
    }
}
