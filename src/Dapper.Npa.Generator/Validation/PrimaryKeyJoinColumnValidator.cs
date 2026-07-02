using Dapper.Npa.Generator.Models;
using Dapper.Npa.Generator.Utils;

namespace Dapper.Npa.Generator.Validation;

using Generator.Models;
using Generator.Utils;
using Microsoft.CodeAnalysis;

internal static class PrimaryKeyJoinColumnValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx, Compilation compilation)
    {
        var pkJoinRels = entity.Relationships.Where(r => r.IsPrimaryKeyJoin).ToList();
        foreach (var rel in pkJoinRels)
        {
            if (symbol.GetMembers(rel.PropertyName).OfType<IPropertySymbol>().FirstOrDefault() is { } propSymbol)
            {
                if (SyntaxHelper.HasAttribute(propSymbol, SyntaxHelper.JoinColumnAttr))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.PrimaryKeyJoinAndJoinColumn,
                        propSymbol.Locations.FirstOrDefault(),
                        rel.PropertyName,
                        entity.ClassName));
                }
            }

            if (string.IsNullOrEmpty(rel.TargetEntity))
                continue;

            var childSymbol = ResolveChildSymbol(rel.TargetEntity, compilation);
            if (childSymbol is null)
                continue;

            if (GetIdGenerationStrategy(childSymbol) != "Assigned")
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.PrimaryKeyJoinNotAssigned,
                    symbol.Locations.FirstOrDefault(),
                    childSymbol.Name));
            }
        }
    }

    private static INamedTypeSymbol? ResolveChildSymbol(string targetFqn, Compilation compilation)
    {
        var metadataName = ToMetadataName(targetFqn);
        return compilation.GetTypeByMetadataName(metadataName)
            ?? compilation.GetTypeByMetadataName(NormalizeGlobalPrefix(targetFqn));
    }

    private static string? GetIdGenerationStrategy(INamedTypeSymbol entity)
    {
        foreach (var member in entity.GetMembers())
        {
            if (member is not IPropertySymbol prop)
                continue;
            if (!SyntaxHelper.HasAttribute(prop, SyntaxHelper.IdAttr))
                continue;

            var genAttr = SyntaxHelper.GetAttribute(prop, SyntaxHelper.GeneratedValueAttr);
            if (genAttr is null)
                return null;

            var strategyVal = genAttr.ConstructorArguments.FirstOrDefault().Value;
            return strategyVal switch
            {
                0 => "Identity",
                1 => "Sequence",
                2 => "Uuid",
                3 => "Assigned",
                _ => "Identity",
            };
        }

        return null;
    }

    private static string ToMetadataName(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring("global::".Length) : fqn;

    private static string NormalizeGlobalPrefix(string fqn)
        => fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn : $"global::{fqn}";
}
