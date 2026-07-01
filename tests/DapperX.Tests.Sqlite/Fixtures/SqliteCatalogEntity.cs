using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Sqlite.Fixtures;

[Entity]
[Table("catalog_items")]
public class CatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Sku { get; set; } = string.Empty;
}

[Repository]
public interface ICatalogItemRepository : IRepository<CatalogItem, int>
{
    Task<IEnumerable<CatalogItem>> FindBySkuAsync(string sku);
    Task<IEnumerable<CatalogItem>> FindAllBySkuAsync(string sku);
}
