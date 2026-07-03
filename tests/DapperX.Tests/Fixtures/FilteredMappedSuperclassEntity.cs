using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[MappedSuperclass]
[GlobalFilter("FromBase", "status = 'active'")]
public abstract class FilteredEntityBase
{
    [Column]
    public string Status { get; set; } = string.Empty;
}

[Entity]
[Table("filtered_super_items")]
public class FilteredSuperItem : FilteredEntityBase
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Title { get; set; } = string.Empty;
}

[Repository]
public interface IFilteredSuperItemRepository : IRepository<FilteredSuperItem, int>
{
}
