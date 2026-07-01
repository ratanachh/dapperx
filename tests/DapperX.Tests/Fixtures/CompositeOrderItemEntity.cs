using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

public class CompositeOrderItemId
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
}

[Entity]
[Table("composite_order_items")]
[IdClass(typeof(CompositeOrderItemId))]
public class CompositeOrderItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    [Column(Name = "product_id")]
    public int ProductId { get; set; }

    [Column]
    public int Quantity { get; set; }
}

[Repository]
public interface ICompositeOrderItemRepository : IRepository<CompositeOrderItem, CompositeOrderItemId>
{
}
