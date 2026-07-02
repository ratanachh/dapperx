using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("orders")]
[NamedEntityGraph("order.withLines", SubGraphs = ["Items"])]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [PostLoad]
    public void OnPostLoad() { }

    [OneToMany(MappedBy = nameof(OrderItem.Order), Cascade = CascadeType.All)]
    [OrderColumn("position")]
    public LazyCollection<OrderItem> Items { get; set; } = new();
}

[Entity]
[Table("order_items")]
public class OrderItem
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
    public Order Order { get; set; } = null!;
}

[Repository]
public interface IOrderRepository : IRepository<Order, int>
{
}
