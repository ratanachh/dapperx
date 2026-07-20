using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.SqlServer.SampleApp.Entities;

[Entity]
[Table("sample_graph_parents")]
public class GraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(GraphChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<GraphChild> Children { get; set; } = new();
}

[Entity]
[Table("sample_graph_children")]
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
