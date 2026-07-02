using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("numbered_items")]
[SequenceGenerator("item_seq", "item_id_seq")]
public class NumberedItem
{
    [Id]
    [GeneratedValue(GenerationType.Sequence, Generator = "item_seq")]
    public long Id { get; set; }

    [Column]
    public string Label { get; set; } = string.Empty;
}

[Repository]
public interface INumberedItemRepository : IRepository<NumberedItem, long>
{
}
