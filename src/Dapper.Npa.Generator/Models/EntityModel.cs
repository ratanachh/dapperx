namespace Dapper.Npa.Generator.Models;

using System.Linq;

internal sealed class EntityModel
{
    public string Namespace { get; init; } = string.Empty;
    public string ClassName { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public string? Schema { get; init; }
    public bool IsImmutable { get; init; }
    public bool IsMappedSuperclass { get; init; }
    public string? SoftDeleteColumn { get; init; }
    public string? DeletedAtColumn { get; init; }
    public SoftDeleteModel? SoftDelete { get; init; }
    public string? TenantIdColumn { get; init; }
    public TenancyModel? Tenancy { get; init; }
    public IReadOnlyList<PropertyModel> Properties { get; init; } = [];
    public IReadOnlyList<FormulaModel> Formulas { get; init; } = [];
    public IReadOnlyList<RelationshipModel> Relationships { get; init; } = [];
    public IReadOnlyList<SecondaryTableModel> SecondaryTables { get; init; } = [];
    public IReadOnlyList<GlobalFilterModel> GlobalFilters { get; init; } = [];
    public IReadOnlyList<NamedQueryModel> NamedQueries { get; init; } = [];
    public IReadOnlyList<string> EntityListeners { get; init; } = [];
    public IReadOnlyList<EntityListenerModel> EntityListenerTypes { get; init; } = [];
    public AuditingModel? Auditing { get; init; }
    public SequenceModel? Sequence { get; init; }
    public IReadOnlyList<AssociationOverrideModel> AssociationOverrides { get; init; } = [];
    public IReadOnlyList<LifecycleMethodModel> LifecycleMethods { get; init; } = [];
    public IReadOnlyList<DerivedQueryPathModel> DerivedQueryPaths { get; init; } = [];
    public IReadOnlyList<ElementCollectionModel> ElementCollections { get; init; } = [];
    public IReadOnlyList<NamedEntityGraphModel> NamedEntityGraphs { get; init; } = [];
    public string FullyQualifiedName => string.IsNullOrEmpty(Namespace) ? ClassName : $"{Namespace}.{ClassName}";
    public string RepositoryImplClassName => $"{ClassName}RepositoryImpl";
    public bool HasLifecycleHook(LifecycleKind kind)
        => LifecycleMethods.Any(m => m.Kind == kind)
           || EntityListenerTypes.Any(l => l.Methods.Any(m => m.Kind == kind));

    public bool HasPostLoad => HasLifecycleHook(LifecycleKind.PostLoad);
    public bool HasLifecycle => LifecycleMethods.Count > 0 || EntityListenerTypes.Count > 0;
    public bool HasRemoveHooks =>
        HasLifecycleHook(LifecycleKind.PreRemove) || HasLifecycleHook(LifecycleKind.PostRemove);

    public bool HasBatchLifecycle => LifecycleMethods.Any(m =>
        m.Kind is LifecycleKind.PrePersistBatch or LifecycleKind.PostPersistBatch
            or LifecycleKind.PreUpdateBatch or LifecycleKind.PostUpdateBatch
            or LifecycleKind.PreRemoveBatch or LifecycleKind.PostRemoveBatch);

    /// <summary>True when entity uses composite PK ([IdClass], [EmbeddedId], or multiple [Id] properties).</summary>
    public bool HasCompositeKey { get; init; }

    public CompositeKeyModel? CompositeKey { get; init; }

    public int IdPropertyCount => Properties.Count(p => p.IsId);

    public IReadOnlyList<EmbeddedModel> EmbeddedSites { get; init; } = [];

    public bool HasEmbeddedColumns => Properties.Any(p => p.IsEmbeddedColumn);

    public bool HasConverters => Properties.Any(p => p.ConverterTypeName is not null);

    /// <summary>Entity reads/writes use a flat DbRow when embed or converter mapping is required.</summary>
    public bool RequiresDbRow => HasEmbeddedColumns || HasConverters;
}
