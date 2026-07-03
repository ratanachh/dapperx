using System.Data;
using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Shared.Fixtures;

[Entity]
[Table("matrix_generated_orders")]
public class MatrixGeneratedOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Generated(GenerationTime.Insert)]
    [Column(Name = "created_at")]
    public DateTime CreatedAt { get; set; }

    [Generated(GenerationTime.Always)]
    [Column(Name = "total_with_tax")]
    public decimal TotalWithTax { get; set; }
}

[Repository]
public interface IMatrixGeneratedOrderRepository : IRepository<MatrixGeneratedOrder, int>
{
}

[Entity]
[Table("matrix_archived_items")]
[SoftDelete]
public class MatrixArchivedItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }
}

[Repository]
public interface IMatrixArchivedItemRepository : IRepository<MatrixArchivedItem, int>
{
    [Query("SELECT a FROM MatrixArchivedItem a WHERE a.Name = :name")]
    Task<IEnumerable<MatrixArchivedItem>> FindByNameCpqlAsync(
        string name,
        bool includeDeleted = false,
        IDbTransaction? transaction = null,
        CancellationToken ct = default);
}

[Entity]
[Table("matrix_filtered_catalog")]
[GlobalFilter("ActiveOnly", "is_active = 1")]
public class MatrixFilteredCatalog
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixFilteredCatalogRepository : IRepository<MatrixFilteredCatalog, int>
{
    Task<IEnumerable<MatrixFilteredCatalog>> FindByNameAsync(string name);
}

[Entity]
[Table("matrix_audited_products")]
public class MatrixAuditedProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [CreatedDate]
    public DateTime CreatedAt { get; set; }

    [CreatedBy]
    public string CreatedBy { get; set; } = string.Empty;

    [LastModifiedDate]
    public DateTime ModifiedAt { get; set; }

    [LastModifiedBy]
    public string ModifiedBy { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixAuditedProductRepository : IRepository<MatrixAuditedProduct, int>
{
}

[Entity]
[Table("matrix_bulk_shipments")]
public class MatrixBulkShipment
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public long Id { get; set; }

    [Column]
    public string TrackingCode { get; set; } = string.Empty;

    [Column]
    public int WeightGrams { get; set; }
}

[Repository]
public interface IMatrixBulkShipmentRepository : IRepository<MatrixBulkShipment, long>
{
}

[Entity]
[Table("matrix_tenant_items")]
public class MatrixTenantItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [TenantId]
    public Guid TenantId { get; set; }
}

[Repository]
public interface IMatrixTenantItemRepository : IRepository<MatrixTenantItem, int>
{
}

[Entity]
[Table("matrix_tenant_region_users")]
[SoftDelete]
[GlobalFilter("active_region", "region = @region")]
public class MatrixTenantRegionUser
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [Column]
    public string Region { get; set; } = "US";

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }

    [TenantId]
    [Column(Name = "tenant_id")]
    public Guid TenantId { get; set; }
}

[Repository]
public interface IMatrixTenantRegionUserRepository : IRepository<MatrixTenantRegionUser, int>
{
}

#if !DAPPERX_PROVIDER_SQLITE
[Entity]
[Table("matrix_numbered_items")]
[SequenceGenerator("matrix_item_seq", "matrix_item_id_seq")]
public class MatrixNumberedItem
{
    [Id]
    [GeneratedValue(GenerationType.Sequence, Generator = "matrix_item_seq")]
    public long Id { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;
}

[Repository]
public interface IMatrixNumberedItemRepository : IRepository<MatrixNumberedItem, long>
{
}
#endif
