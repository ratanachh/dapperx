namespace DapperX.Generator.Models;

internal sealed class RelationshipModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty; // "OneToMany","ManyToOne","OneToOne","ManyToMany"
    public string? TargetEntity { get; init; }
    public string? ForeignKeyColumn { get; set; }
    public string? MappedBy { get; init; }
    public int CascadeFlags { get; init; }
    public string Fetch { get; init; } = "Lazy";
    public bool IsPrimaryKeyJoin { get; init; }
    public string? MapKeyColumn { get; init; }
    public string? JoinTable { get; init; }
    public string? JoinTableFk { get; init; }
    public string? JoinTableInverseFk { get; init; }
    public string? OrderByClause { get; init; }
    public string? OrderColumnName { get; init; }

    // Enriched at compile time (second pass) for batch loaders
    public bool IsLazyCollection { get; init; }
    public bool IsLazyMap { get; init; }
    public bool IsLazyReference { get; init; }
    public string? ChildEntityFqn { get; set; }
    public string? ChildTableName { get; set; }
    public string? ChildSchema { get; set; }
    public string? FkPropertyNameOnChild { get; set; }
    public string? MapKeyPropertyName { get; set; }
    public string? MapKeyClrTypeName { get; init; }
    public bool ChildHasPostLoad { get; set; }
    public bool IsBatchLoadable { get; set; }
}
