using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("batch_graph_parents")]
public class BatchGraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [PrePersistBatch]
    public void OnPrePersistBatch() { }

    [PostPersistBatch]
    public void OnPostPersistBatch() { }

    [PreRemoveBatch]
    public void OnPreRemoveBatch() { }

    [PostRemoveBatch]
    public void OnPostRemoveBatch() { }

    [OneToMany(MappedBy = nameof(BatchGraphChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<BatchGraphChild> Children { get; set; } = new();
}

[Entity]
[Table("batch_graph_children")]
public class BatchGraphChild
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
    public BatchGraphParent Parent { get; set; } = null!;
}

[Repository]
public interface IBatchGraphParentRepository : IRepository<BatchGraphParent, int>
{
}

[Entity]
[SoftDelete]
[Table("soft_delete_graph_orders")]
[NamedEntityGraph("softDeleteOrder.withLines", SubGraphs = ["Lines"])]
public class SoftDeleteGraphOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [OneToMany(MappedBy = nameof(SoftDeleteGraphOrderLine.Order), Cascade = CascadeType.All)]
    public LazyCollection<SoftDeleteGraphOrderLine> Lines { get; set; } = new();
}

[Entity]
[SoftDelete]
[Table("soft_delete_graph_order_lines")]
public class SoftDeleteGraphOrderLine
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public SoftDeleteGraphOrder Order { get; set; } = null!;
}

[Repository]
public interface ISoftDeleteGraphOrderRepository : IRepository<SoftDeleteGraphOrder, int>
{
}

[Entity]
[Table("versioned_graph_orders")]
public class VersionedGraphOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Version]
    [Column(Name = "row_version")]
    public int RowVersion { get; set; }

    [OneToMany(MappedBy = nameof(VersionedGraphOrderLine.Order), Cascade = CascadeType.All)]
    public LazyCollection<VersionedGraphOrderLine> Lines { get; set; } = new();
}

[Entity]
[Table("versioned_graph_order_lines")]
public class VersionedGraphOrderLine
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Version]
    [Column(Name = "row_version")]
    public int RowVersion { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public VersionedGraphOrder Order { get; set; } = null!;
}

[Repository]
public interface IVersionedGraphOrderRepository : IRepository<VersionedGraphOrder, int>
{
}
