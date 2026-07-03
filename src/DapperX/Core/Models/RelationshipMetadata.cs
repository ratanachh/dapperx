using DapperX.Core.Enums;

namespace DapperX.Core.Models;

using Core.Enums;

public sealed class RelationshipMetadata
{
    public string PropertyName { get; init; } = string.Empty;
    public RelationshipType RelationshipType { get; init; }
    public string? TargetEntity { get; init; }
    public string? ForeignKeyColumn { get; init; }
    public string? MappedBy { get; init; }
    public CascadeType Cascade { get; init; }
    public FetchType Fetch { get; init; } = FetchType.Lazy;
    public bool IsPrimaryKeyJoin { get; init; }
    public string? MapKeyColumn { get; init; }
    public string? JoinTable { get; init; }
    public string? JoinTableFk { get; init; }
    public string? JoinTableInverseFk { get; init; }
    public string? OrderByClause { get; init; }
    public string? OrderColumnName { get; init; }
}

public enum RelationshipType { OneToMany, ManyToOne, OneToOne, ManyToMany }
