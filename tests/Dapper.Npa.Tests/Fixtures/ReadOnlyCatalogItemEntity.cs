using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("read_only_catalog_items")]
[Immutable]
public class ReadOnlyCatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface IReadOnlyCatalogItemRepository : IRepository<ReadOnlyCatalogItem, int>
{
}
