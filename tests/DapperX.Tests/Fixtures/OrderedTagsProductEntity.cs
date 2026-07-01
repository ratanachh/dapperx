using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("ordered_tagged_products")]
public class OrderedTagsProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [ElementCollection]
    [CollectionTable("ordered_product_tags", JoinColumn = "product_id")]
    [Column(Name = "tag")]
    [OrderColumn("position")]
    public LazyCollection<string> Tags { get; set; } = new();
}

[Repository]
public interface IOrderedTagsProductRepository : IRepository<OrderedTagsProduct, int>
{
}
