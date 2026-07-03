using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.IntegrationTests.Shared.Fixtures;

[Entity]
[Table("integ_catalog")]
public class IntegCatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Sku { get; set; } = string.Empty;
}

[Repository]
public interface IIntegCatalogItemRepository : IRepository<IntegCatalogItem, int>
{
    Task<IEnumerable<IntegCatalogItem>> FindBySkuAsync(string sku);
    Task<IEnumerable<IntegCatalogItem>> FindBySkuAsync(string sku, Sort sort, Pageable pageable);
}
