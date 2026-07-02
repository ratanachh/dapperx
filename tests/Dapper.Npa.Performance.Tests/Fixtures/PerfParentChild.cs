using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Performance.Tests.Fixtures;

[Entity]
[Table("perf_parents")]
public class PerfParent
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = "Parent")]
    public LazyCollection<PerfChild> Children { get; set; } = null!;
}

[Entity]
[Table("perf_children")]
public class PerfChild
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "parent_id")]
    public int ParentId { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("parent_id")]
    public PerfParent Parent { get; set; } = null!;
}

[Repository]
public interface IPerfParentRepository : IRepository<PerfParent, int>
{
}
