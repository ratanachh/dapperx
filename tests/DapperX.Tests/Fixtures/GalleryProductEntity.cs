using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.Relations.Lazy;

namespace DapperX.Tests.Fixtures;

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
