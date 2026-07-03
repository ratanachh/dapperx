using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Shared.Fixtures;

/// <summary>Minimal catalog entity compiled in all four provider test assemblies for matrix-4 SQL assertions.</summary>
[Entity]
[Table("matrix_catalog")]
public class MatrixCatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Sku { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixCatalogItemRepository : IRepository<MatrixCatalogItem, int>
{
    Task<IEnumerable<MatrixCatalogItem>> FindBySkuAsync(string sku);
    Task<IEnumerable<MatrixCatalogItem>> FindBySkuOrderBySkuDescAsync(string sku);
    Task<IEnumerable<MatrixCatalogItem>> FindBySkuAsync(string sku, Sort sort);
    Task<IEnumerable<MatrixCatalogItem>> FindBySkuAsync(string sku, Sort sort, Pageable pageable);
    Task<Page<MatrixCatalogItem>> FindBySkuPagedAsync(string sku, Pageable pageable);
}
