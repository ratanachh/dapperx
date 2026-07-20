using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.SqlServer.SampleApp.Entities;

[Entity]
[Table("catalog_products")]
public class CatalogProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Sku { get; set; } = string.Empty;

    [Column]
    [Sortable]
    public string Name { get; set; } = string.Empty;

    [Column]
    [Sortable]
    public string Category { get; set; } = string.Empty;

    [Column]
    public decimal Price { get; set; }

    [Column(Name = "in_stock")]
    public bool InStock { get; set; } = true;

    [Column]
    public string Status { get; set; } = "Active";

    [Column(Name = "secret_payload")]
    [ColumnTransformer(Read = "secret_payload", Write = "?")]
    public string EncryptedPayload { get; set; } = string.Empty;

    [CreatedDate]
    [Column(Name = "created_at")]
    public DateTime CreatedAt { get; set; }

    [LastModifiedDate]
    [Column(Name = "updated_at")]
    public DateTime UpdatedAt { get; set; }
}
