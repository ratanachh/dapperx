using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("tenant_scoped_items")]
[NamedEntityGraph("tenantItem.default")]
public class TenantScopedItem
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
public interface ITenantScopedItemRepository : IRepository<TenantScopedItem, int>
{
}

[MappedSuperclass]
public abstract class TenantScopedBase
{
    [TenantId]
    public Guid TenantId { get; set; }
}

[Entity]
[Table("mapped_tenant_items")]
public class MappedTenantItem : TenantScopedBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IMappedTenantItemRepository : IRepository<MappedTenantItem, int>
{
}
