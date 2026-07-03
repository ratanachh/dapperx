using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.SampleApp.Entities;

namespace DapperX.SampleApp.Repositories;

[Repository]
public interface ICatalogProductRepository : IRepository<CatalogProduct, int>
{
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByInStockAndPriceLessThanAsync(bool inStock, decimal price, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryOrderByPriceDescAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, Sort sort, Pageable pageable, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryLockedAsync(string category, LockMode lockMode, CancellationToken ct = default);

}
