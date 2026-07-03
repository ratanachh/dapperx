using DapperX.Generator.Models;
using DapperX.Generator.Utils;

namespace DapperX.Generator.Builders;

using Generator.Models;
using Generator.Utils;

internal enum GraphCascadeOperation
{
    Insert,
    Update,
    Delete,
}

internal sealed class GraphRelationshipInfo
{
    public RelationshipModel Relationship { get; init; } = null!;
    public EntityModel ChildEntity { get; init; } = null!;
    public string ChildImplTypeName { get; init; } = string.Empty;
    public string ChildFqn { get; init; } = string.Empty;
    public string FkPropertyName { get; init; } = string.Empty;
}

internal static class GraphBuilder
{
    public static IReadOnlyList<GraphRelationshipInfo> GetGraphRelationships(
        EntityModel entity,
        IReadOnlyDictionary<string, EntityModel> allModels,
        GraphCascadeOperation operation)
    {
        var result = new List<GraphRelationshipInfo>();
        foreach (var rel in entity.Relationships.Where(r => MatchesOneToManyGraph(r, operation)))
        {
            TryAddGraphRelationship(entity, rel, allModels, result);
        }

        return result;
    }

    public static IReadOnlyList<RelationshipModel> GetManyToManyGraphRelationships(
        EntityModel entity,
        GraphCascadeOperation operation)
        => entity.Relationships
            .Where(r => r.Kind == "ManyToMany"
                        && r.IsLazyCollection
                        && !string.IsNullOrEmpty(r.JoinTable)
                        && MatchesManyToManyCascade(r, operation))
            .ToList();

    public static bool HasCycles(EntityModel root, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return Visit(root.FullyQualifiedName, allModels, visiting, visited);
    }

    private static bool MatchesOneToManyGraph(RelationshipModel rel, GraphCascadeOperation operation)
    {
        if (rel.Kind != "OneToMany" || !rel.IsLazyCollection)
            return false;

        return operation switch
        {
            GraphCascadeOperation.Insert => CascadeHelper.HasPersist(rel.CascadeFlags),
            GraphCascadeOperation.Update => CascadeHelper.HasMerge(rel.CascadeFlags),
            GraphCascadeOperation.Delete => CascadeHelper.HasRemove(rel.CascadeFlags),
            _ => false,
        };
    }

    private static bool MatchesManyToManyCascade(RelationshipModel rel, GraphCascadeOperation operation)
        => operation switch
        {
            GraphCascadeOperation.Insert => CascadeHelper.HasPersist(rel.CascadeFlags),
            GraphCascadeOperation.Update => CascadeHelper.HasMerge(rel.CascadeFlags),
            GraphCascadeOperation.Delete => CascadeHelper.HasRemove(rel.CascadeFlags),
            _ => false,
        };

    private static void TryAddGraphRelationship(
        EntityModel entity,
        RelationshipModel rel,
        IReadOnlyDictionary<string, EntityModel> allModels,
        List<GraphRelationshipInfo> result)
    {
        var childFqn = rel.ChildEntityFqn ?? rel.TargetEntity;
        if (string.IsNullOrEmpty(childFqn))
            return;

        var child = ResolveEntity(childFqn!, allModels);
        if (child is null)
            return;

        var fkProp = rel.FkPropertyNameOnChild
            ?? child.Properties.FirstOrDefault(p => p.ColumnName == rel.ForeignKeyColumn)?.PropertyName;
        if (string.IsNullOrEmpty(fkProp))
            return;

        result.Add(new GraphRelationshipInfo
        {
            Relationship = rel,
            ChildEntity = child,
            ChildImplTypeName = ResolveImplTypeName(child),
            ChildFqn = child.FullyQualifiedName,
            FkPropertyName = fkProp!,
        });
    }

    private static bool Visit(
        string entityFqn,
        IReadOnlyDictionary<string, EntityModel> allModels,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visiting.Contains(entityFqn))
            return true;
        if (visited.Contains(entityFqn))
            return false;

        visiting.Add(entityFqn);

        var entity = allModels.Values.FirstOrDefault(e => e.FullyQualifiedName == entityFqn);
        if (entity is not null)
        {
            foreach (var rel in entity.Relationships.Where(r =>
                         r.Kind == "OneToMany" && r.IsLazyCollection && r.CascadeFlags != CascadeHelper.None))
            {
                var childFqn = rel.ChildEntityFqn ?? rel.TargetEntity;
                if (!string.IsNullOrEmpty(childFqn) && Visit(childFqn!, allModels, visiting, visited))
                    return true;
            }
        }

        visiting.Remove(entityFqn);
        visited.Add(entityFqn);
        return false;
    }

    private static string ResolveImplTypeName(EntityModel child)
    {
        var impl = RepositoryNaming.DefaultImplClassName(child.ClassName);
        var ns = string.IsNullOrEmpty(child.Namespace)
            ? "Generated"
            : $"{child.Namespace}.Generated";
        return $"{ns}.{impl}";
    }

    private static EntityModel? ResolveEntity(string fqn, IReadOnlyDictionary<string, EntityModel> allModels)
    {
        var key = fqn.StartsWith("global::", StringComparison.Ordinal) ? fqn.Substring(8) : fqn;
        if (allModels.TryGetValue(key, out var model))
            return model;

        return allModels.Values.FirstOrDefault(m =>
            m.FullyQualifiedName == key || m.ClassName == key);
    }
}
