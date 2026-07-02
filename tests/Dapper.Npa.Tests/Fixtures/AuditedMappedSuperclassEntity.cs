using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[MappedSuperclass]
public abstract class AuditedEntityBase
{
    [CreatedDate]
    public DateTime CreatedAt { get; set; }

    [CreatedBy]
    public string CreatedBy { get; set; } = string.Empty;

    [LastModifiedDate]
    public DateTime ModifiedAt { get; set; }

    [LastModifiedBy]
    public string ModifiedBy { get; set; } = string.Empty;
}

[Entity]
[Table("mapped_audit_items")]
public class MappedAuditItem : AuditedEntityBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IMappedAuditItemRepository : IRepository<MappedAuditItem, int>
{
}
