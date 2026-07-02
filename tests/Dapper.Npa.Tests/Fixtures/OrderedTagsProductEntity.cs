using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

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
