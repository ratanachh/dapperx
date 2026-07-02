using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.Relations.Lazy;

namespace Dapper.Npa.IntegrationTests.Shared.Fixtures;

[Entity]
[Table("integ_archived")]
[SoftDelete]
public class IntegArchivedItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column(Name = "is_deleted")]
    public bool IsDeleted { get; set; }
}

[Repository]
public interface IIntegArchivedItemRepository : IRepository<IntegArchivedItem, int>
{
}

[Entity]
[Table("integ_tenant_items")]
public class IntegTenantItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [TenantId]
    public Guid TenantId { get; set; }
}

[Repository]
public interface IIntegTenantItemRepository : IRepository<IntegTenantItem, int>
{
}

[Entity]
[Table("integ_audited")]
public class IntegAuditedItem
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
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
public interface IIntegAuditedItemRepository : IRepository<IntegAuditedItem, int>
{
}

[Entity]
[Table("integ_bulk")]
public class IntegBulkRow
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public long Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Repository]
public interface IIntegBulkRowRepository : IRepository<IntegBulkRow, long>
{
}

[Entity]
[Table("integ_parents")]
public class IntegParent
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(IntegChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<IntegChild> Children { get; set; } = new();
}

[Entity]
[Table("integ_children")]
public class IntegChild
{
    [Id]
    [GeneratedValue(GenerationType.Assigned)]
    public int Id { get; set; }

    [Column(Name = "parent_id")]
    public int ParentId { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("parent_id")]
    public IntegParent Parent { get; set; } = null!;
}

[Repository]
public interface IIntegParentRepository : IRepository<IntegParent, int>
{
}
