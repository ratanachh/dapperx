using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("tenant_region_users")]
[SoftDelete]
[GlobalFilter("active_region", "region = @region")]
public class TenantRegionUser
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
public interface ITenantRegionUserRepository : IRepository<TenantRegionUser, int>
{
}
