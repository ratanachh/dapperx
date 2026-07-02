using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("tagged_products")]
public class TaggedProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [ElementCollection]
    [CollectionTable("product_tags", JoinColumn = "product_id")]
    public LazyCollection<string> Tags { get; set; } = new();
}

[Repository]
public interface ITaggedProductRepository : IRepository<TaggedProduct, int>
{
}
