using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Shared.Fixtures;

[Entity]
[Table("matrix_graph_parents")]
public class MatrixGraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(MatrixGraphChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<MatrixGraphChild> Children { get; set; } = new();
}

[Entity]
[Table("matrix_graph_children")]
public class MatrixGraphChild
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
    public MatrixGraphParent Parent { get; set; } = null!;
}

[Repository]
public interface IMatrixGraphParentRepository : IRepository<MatrixGraphParent, int>
{
}

[Entity]
[SoftDelete]
[Table("matrix_graph_orders")]
[NamedEntityGraph("matrixGraphOrder.withLines", SubGraphs = ["Lines"])]
public class MatrixGraphOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [OneToMany(MappedBy = nameof(MatrixGraphOrderLine.Order), Cascade = CascadeType.All)]
    public LazyCollection<MatrixGraphOrderLine> Lines { get; set; } = new();
}

[Entity]
[SoftDelete]
[Table("matrix_graph_order_lines")]
public class MatrixGraphOrderLine
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
    public MatrixGraphOrder Order { get; set; } = null!;
}

[Repository]
public interface IMatrixGraphOrderRepository : IRepository<MatrixGraphOrder, int>
{
}

[Entity]
[Table("matrix_orders")]
[NamedEntityGraph("matrixOrder.withItems", SubGraphs = ["Items"])]
public class MatrixOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(MatrixOrderItem.Order), Cascade = CascadeType.All)]
    [OrderColumn("position")]
    public LazyCollection<MatrixOrderItem> Items { get; set; } = new();
}

[Entity]
[Table("matrix_order_items")]
public class MatrixOrderItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column(Name = "position")]
    public int Position { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public MatrixOrder Order { get; set; } = null!;
}

[Repository]
public interface IMatrixOrderRepository : IRepository<MatrixOrder, int>
{
}
