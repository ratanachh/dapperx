using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("tenant_orders")]
public class TenantOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [TenantId]
    public Guid TenantId { get; set; }

    [OneToMany(MappedBy = nameof(TenantOrderLine.Order), Cascade = CascadeType.All)]
    public LazyCollection<TenantOrderLine> Lines { get; set; } = new();
}

[Entity]
[Table("tenant_order_lines")]
public class TenantOrderLine
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [TenantId]
    public Guid TenantId { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public TenantOrder Order { get; set; } = null!;
}

[Repository]
public interface ITenantOrderRepository : IRepository<TenantOrder, int>
{
}
