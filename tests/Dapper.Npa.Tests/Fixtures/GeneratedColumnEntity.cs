using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("generated_orders")]
public class GeneratedOrder
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
public interface IGeneratedOrderRepository : IRepository<GeneratedOrder, int>
{
}

[MappedSuperclass]
public abstract class GeneratedTimestampsBase
{
    [Generated(GenerationTime.Insert)]
    [Column(Name = "created_at")]
    public DateTime CreatedAt { get; set; }
}

[Entity]
[Table("mapped_generated_items")]
public class MappedGeneratedItem : GeneratedTimestampsBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IMappedGeneratedItemRepository : IRepository<MappedGeneratedItem, int>
{
}
