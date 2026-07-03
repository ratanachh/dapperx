using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Validation;

using Microsoft.CodeAnalysis;
using Generator.Models;
using Generator.Utils;

internal static class NamedEntityGraphValidator
{
    public static void Validate(EntityModel entity, INamedTypeSymbol symbol, SourceProductionContext ctx)
    {
        var relationshipNames = new HashSet<string>(
            entity.Relationships.Select(r => r.PropertyName),
            StringComparer.Ordinal);

        foreach (var graph in entity.NamedEntityGraphs)
        {
            foreach (var node in graph.AttributeNodes)
            {
                if (!relationshipNames.Contains(node))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.UnknownEntityGraphNode,
                        symbol.Locations.FirstOrDefault(),
                        graph.Name,
                        entity.ClassName,
                        node));
                }
            }

            foreach (var subGraph in graph.SubGraphs)
            {
                if (!relationshipNames.Contains(subGraph.RelationshipProperty))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.UnknownEntityGraphNode,
                        symbol.Locations.FirstOrDefault(),
                        graph.Name,
                        entity.ClassName,
                        subGraph.RelationshipProperty));
                }
            }
        }
    }

    public static void ValidateSubGraphNodes(
        EntityModel entity,
        INamedTypeSymbol symbol,
        IReadOnlyDictionary<string, EntityModel> allModels,
        SourceProductionContext ctx)
    {
        foreach (var graph in entity.NamedEntityGraphs)
        {
            foreach (var subGraph in graph.SubGraphs)
            {
                var rel = entity.Relationships.FirstOrDefault(r => r.PropertyName == subGraph.RelationshipProperty);
                if (rel is null || !TryResolveTarget(rel, allModels, out var targetEntity))
                    continue;

                var targetRelNames = new HashSet<string>(
                    targetEntity.Relationships.Select(r => r.PropertyName),
                    StringComparer.Ordinal);

                foreach (var node in subGraph.AttributeNodes)
                {
                    if (!targetRelNames.Contains(node))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.UnknownEntityGraphSubGraphNode,
                            symbol.Locations.FirstOrDefault(),
                            graph.Name,
                            entity.ClassName,
                            subGraph.RelationshipProperty,
                            node));
                    }
                }
            }
        }
    }

    private static bool TryResolveTarget(
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> allModels,
        out EntityModel targetModel)
    {
        targetModel = null!;
        var fqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(fqn))
            return false;

        var key = fqn!.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (allModels.TryGetValue(key, out targetModel!))
            return true;

        targetModel = allModels.Values.FirstOrDefault(m =>
            m.FullyQualifiedName == key || m.ClassName == key)!;
        return targetModel is not null;
    }
}
