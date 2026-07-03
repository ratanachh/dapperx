namespace DapperX.Core.Models;

public sealed class EntityMetadata
{
    public string EntityName { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
    public string? Schema { get; init; }
    public bool IsImmutable { get; init; }
    public bool IsMappedSuperclass { get; init; }
    public string? SoftDeleteColumn { get; init; }
    public string? DeletedAtColumn { get; init; }
    public string? TenantIdColumn { get; init; }
    public IReadOnlyList<PropertyMetadata> Properties { get; init; } = [];
    public IReadOnlyList<RelationshipMetadata> Relationships { get; init; } = [];
    public IReadOnlyList<IndexMetadata> Indexes { get; init; } = [];
    public IReadOnlyList<UniqueConstraintInfo> UniqueConstraints { get; init; } = [];
    public IReadOnlyList<GlobalFilterInfo> GlobalFilters { get; init; } = [];
    public IReadOnlyList<SecondaryTableMetadata> SecondaryTables { get; init; } = [];
    public IReadOnlyList<NamedQueryMetadata> NamedQueries { get; init; } = [];
    public AuditingMetadata? Auditing { get; init; }
    public SequenceMetadata? Sequence { get; init; }
}
