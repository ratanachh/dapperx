using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("audited_products")]
public class AuditedProduct
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
public interface IAuditedProductRepository : IRepository<AuditedProduct, int>
{
}
