# Soft Delete, Multi-Tenancy & Auditing

These three cross-cutting behaviors compose on the same entity, as in the sample app's `AppUser`:

```csharp
[Entity]
[Table("app_users")]
[SoftDelete]
[GlobalFilter("active_region", "region = @region")]
[EntityListeners(typeof(SampleAuditListener))]
public class AppUser : BaseEntity   // BaseEntity carries [CreatedDate]/[LastModifiedDate]/[CreatedBy]/[LastModifiedBy]/[Version]
{
    [Id] [GeneratedValue(GenerationType.Identity)] public int Id { get; set; }
    [Column] public string Email { get; set; } = string.Empty;
    [Column] public string Region { get; set; } = "US";

    [TenantId] [Column(Name = "tenant_id")] public Guid TenantId { get; set; }
    [Column(Name = "is_deleted")] public bool IsDeleted { get; set; }
}
```

## Soft delete

[`[SoftDelete]`](xref:DapperX.Core.Attributes.SoftDeleteAttribute) turns `DeleteAsync`/`DeleteByIdAsync` into
an `UPDATE` that sets a flag column (`Column`, default `is_deleted`; optionally a `DeletedAtColumn` timestamp)
instead of a `DELETE`. Every generated read query automatically excludes soft-deleted rows unless you pass
`includeDeleted: true` to a repository method, or call `.IncludeDeleted()` on a fluent query:

```csharp
await repo.DeleteByIdAsync(id);                 // sets is_deleted = true, doesn't remove the row
var all = await repo.Query().IncludeDeleted().ToListAsync();
```

## Multi-tenancy

[`[TenantId]`](xref:DapperX.Core.Attributes.TenantIdAttribute) marks the tenant-discriminator column.
Generated queries automatically scope to the current tenant, resolved from an `ITenantProvider` you register
in DI (`SampleTenantProvider` in the sample app). Every write is stamped with the current tenant; every read
is filtered to it — there's no way to accidentally read across tenants through a generated repository method.

## Global filters

[`[GlobalFilter(name, condition)]`](xref:DapperX.Core.Attributes.GlobalFilterAttribute) declares a named,
compile-time SQL condition fragment that can be toggled on/off at runtime via
[`DapperXOptions`](xref:DapperX.Runtime.Configuration.DapperXOptions):

```csharp
options.EnableFilter("active_region", new { region = "US" });
var users = await repo.GetAllAsync();   // WHERE region = @region is now appended

options.DisableFilter("active_region");
```

The condition is a compile-time constant appended conditionally — never runtime string concatenation — so it
can't be used as a SQL injection vector via filter parameters supplied at `EnableFilter` time (they're bound
as regular parameters, not interpolated into the SQL).

## Auditing

`[CreatedDate]`/`[LastModifiedDate]` are stamped with the current timestamp on insert / every write.
`[CreatedBy]`/`[LastModifiedBy]` are stamped with the "current principal" resolved from an `IAuditingProvider`
you register in DI (`SampleAuditingProvider` in the sample app) — typically the current user ID from an
`HttpContext`/`ClaimsPrincipal`. These are usually placed on a shared `[MappedSuperclass]` base (see
[Entity Mapping](entity-mapping.md)) rather than repeated per entity.

## Entity listeners

[`[EntityListeners(typeof(...))]`](xref:DapperX.Core.Attributes.EntityListenersAttribute) attaches
an external listener class implementing lifecycle callbacks (see [Lifecycle Hooks](lifecycle-hooks.md)) — used
in the sample app to count `PrePersist` invocations without modifying the entity itself.
