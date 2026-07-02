using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.IntegrationTests.Shared.Fixtures;

[Entity]
[Table("integ_filtered_catalog")]
[GlobalFilter("ActiveOnly", "is_active = 1")]
public class IntegFilteredCatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column(Name = "is_active")]
    public bool IsActive { get; set; } = true;
}

[Repository]
public interface IIntegFilteredCatalogItemRepository : IRepository<IntegFilteredCatalogItem, int>
{
}

[Entity]
[Table("integ_transform_products")]
public class IntegTransformProduct
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "name")]
    [ColumnTransformer(Read = "name", Write = "?")]
    public string DisplayName { get; set; } = string.Empty;
}

[Repository]
public interface IIntegTransformProductRepository : IRepository<IntegTransformProduct, int>
{
}

[Projection(typeof(IntegCatalogItem))]
public class IntegCatalogSkuDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
}

[Entity]
[Table("integ_query_customers")]
public class IntegQueryCustomer
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Entity]
[Table("integ_query_products")]
public class IntegQueryProduct
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column(Name = "customer_id")]
    public int CustomerId { get; set; }

    [ManyToOne]
    [JoinColumn("customer_id")]
    public IntegQueryCustomer Customer { get; set; } = null!;
}

[Repository]
public interface IIntegQueryProductRepository : IRepository<IntegQueryProduct, int>
{
}

[Repository]
public interface IIntegQueryCustomerRepository : IRepository<IntegQueryCustomer, int>
{
}

[Entity]
[Table("integ_tenant_region_users")]
[SoftDelete]
[GlobalFilter("active_region", "region = @region")]
public class IntegTenantRegionUser
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
public interface IIntegTenantRegionUserRepository : IRepository<IntegTenantRegionUser, int>
{
}
