using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("constraint_products")]
[UniqueConstraint("Sku", Name = "uk_sku")]
[Index("Name", Name = "ix_name")]
public class ConstraintProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface IConstraintProductRepository : IRepository<ConstraintProduct, int>
{
}
