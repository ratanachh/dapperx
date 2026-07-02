using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

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
