using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Shared.Fixtures;

[Entity]
[Table("matrix_cpql_items")]
public class MatrixCpqlItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixCpqlItemRepository : IRepository<MatrixCpqlItem, int>
{
    [Query("SELECT m FROM MatrixCpqlItem m WHERE SUBSTRING(m.Sku, 1, 3) = :prefix")]
    Task<IEnumerable<MatrixCpqlItem>> FindBySkuPrefixCpqlAsync(string prefix);

    [Query("SELECT m FROM MatrixCpqlItem m WHERE m.Sku LIKE :pattern")]
    Task<IEnumerable<MatrixCpqlItem>> FindBySkuLikeCpqlAsync(string pattern);

    [Query("UPDATE MatrixCpqlItem m SET m.Sku = :sku WHERE m.Id = :id")]
    Task<long> UpdateSkuByIdCpqlAsync(int id, string sku);
}

#if !DAPPERX_PROVIDER_SQLITE
[Entity]
[Table("matrix_proc_orders")]
public class MatrixProcOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixProcOrderRepository : IRepository<MatrixProcOrder, int>
{
    [StoredProcedure("sp_matrix_list_proc_orders")]
    Task<IEnumerable<MatrixProcOrder>> ListOrdersSpAsync(int customerId);
}
#endif
