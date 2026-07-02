using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

/// <summary>Parent with OneToMany children but Cascade=None — graph must not cascade to children.</summary>
[Entity]
[Table("graph_parents")]
public class GraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(GraphChild.Parent), Cascade = CascadeType.None)]
    public LazyCollection<GraphChild> Children { get; set; } = new();
}

[Entity]
[Table("graph_children")]
public class GraphChild
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "parent_id")]
    public int ParentId { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("parent_id")]
    public GraphParent Parent { get; set; } = null!;
}

[Repository]
public interface IGraphParentRepository : IRepository<GraphParent, int>
{
}
