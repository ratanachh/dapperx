using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Abstractions.Sorting;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.IntegrationTests.Shared.Fixtures;

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
