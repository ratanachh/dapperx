using Dapper.Npa.SampleApp.Entities;
using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Abstractions.Sorting;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.SampleApp.Repositories;

[Repository]
public interface ICatalogProductRepository : IRepository<CatalogProduct, int>
{
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByInStockAndPriceLessThanAsync(bool inStock, decimal price, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryOrderByPriceDescAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, Sort sort, Pageable pageable, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryLockedAsync(string category, LockMode lockMode, CancellationToken ct = default);

}
