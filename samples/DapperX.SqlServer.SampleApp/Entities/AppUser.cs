using DapperX.Core.Attributes;
using DapperX.Core.Enums;
using DapperX.SqlServer.SampleApp.Infrastructure;

namespace DapperX.SqlServer.SampleApp.Entities;

// DPX013/DPX014 fire unconditionally whenever [CreatedBy]/[LastModifiedBy]/[TenantId] are used — the
// generator has no way to see that IAuditingProvider and ITenantProvider are registered in Program.cs.
#pragma warning disable DPX013, DPX014
[Entity]
[Table("app_users")]
[SoftDelete]
[GlobalFilter("active_region", "region = @region")]
[EntityListeners(typeof(SampleAuditListener))]
public class AppUser : BaseEntity
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [Column]
    public string Region { get; set; } = "US";

    [Column(Name = "address_city")]
    public string AddressCity { get; set; } = string.Empty;

    [Column(Name = "address_country")]
    public string AddressCountry { get; set; } = string.Empty;

    [TenantId]
    [Column(Name = "tenant_id")]
    public Guid TenantId { get; set; }

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }
}
#pragma warning restore DPX013, DPX014
