using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("gallery_products")]
public class GalleryProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [ElementCollection]
    [CollectionTable("product_images", JoinColumn = "product_id")]
    [AttributeOverride(nameof(ProductImage.Url), "image_url")]
    [AttributeOverride(nameof(ProductImage.Caption), "image_caption")]
    public LazyCollection<ProductImage> Images { get; set; } = new();
}

[Repository]
public interface IGalleryProductRepository : IRepository<GalleryProduct, int>
{
}
