using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.SampleApp.Entities;

[Entity]
[Table("sample_orders")]
public class SalesOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [Generated(GenerationTime.Always)]
    [Column(Name = "total_with_tax")]
    public decimal TotalWithTax { get; set; }

    [Formula("(SELECT COUNT(*) FROM sample_order_lines ol WHERE ol.order_id = sample_orders.id)")]
    public int LineCount { get; set; }

    [PrePersist]
    public void OnPrePersist() { }

    [PostPersist]
    public void OnPostPersist() { }

    [OneToMany(MappedBy = nameof(SalesOrderLine.Order), Cascade = CascadeType.All)]
    [OrderColumn("position")]
    public LazyCollection<SalesOrderLine> Lines { get; set; } = new();
}

[Entity]
[Table("sample_order_lines")]
public class SalesOrderLine
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "order_id")]
    public int OrderId { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column]
    public int Position { get; set; }

    [ManyToOne]
    [JoinColumn("order_id")]
    public SalesOrder Order { get; set; } = null!;
}
