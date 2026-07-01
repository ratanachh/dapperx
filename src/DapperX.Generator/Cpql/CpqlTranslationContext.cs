using DapperX.Generator.Models;

namespace DapperX.Generator.Cpql;

internal sealed class CpqlTranslationContext
{
    public EntityModel RootEntity { get; }
    public string Provider { get; }
    public IReadOnlyDictionary<string, EntityModel> AllModels { get; }
    public Dictionary<string, EntityModel> Aliases { get; } = new(StringComparer.Ordinal);
    public List<CpqlJoinNode> ImplicitJoins { get; } = new();
    public List<CpqlJoinNode> ExplicitJoins { get; } = new();
    public HashSet<string> ImplicitJoinKeys { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> CteNames { get; } = new(StringComparer.OrdinalIgnoreCase);
    public int SubqueryDepth { get; set; }

    /// <summary>When false, SELECT translation omits <c>is_deleted = 0</c> (IncludeDeleted bypass).</summary>
    public bool ApplySoftDeleteFilter { get; }

    public CpqlTranslationContext(
        EntityModel rootEntity,
        string provider,
        IReadOnlyDictionary<string, EntityModel> allModels,
        bool applySoftDeleteFilter = true)
    {
        RootEntity = rootEntity;
        Provider = provider;
        AllModels = allModels;
        ApplySoftDeleteFilter = applySoftDeleteFilter;
    }

    public EntityModel? ResolveEntityByName(string name)
    {
        foreach (var model in AllModels.Values)
        {
            if (string.Equals(model.ClassName, name, StringComparison.Ordinal))
                return model;
        }
        return null;
    }

    public EntityModel? GetAliasEntity(string alias)
        => Aliases.TryGetValue(alias, out var e) ? e : null;

    public void RegisterImplicitJoin(string sourceAlias, string relationshipProperty, string joinAlias)
    {
        var key = sourceAlias + "." + relationshipProperty + "->" + joinAlias;
        if (ImplicitJoinKeys.Add(key))
        {
            ImplicitJoins.Add(new CpqlJoinNode
            {
                SourceAlias = sourceAlias,
                RelationshipProperty = relationshipProperty,
                JoinAlias = joinAlias,
            });
        }
    }
}
