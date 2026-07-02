using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

[Entity]
[Table("catalog_items")]
[GlobalFilter("ActiveOnly", "is_active = 1")]
public class CatalogItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface ICatalogItemRepository : IRepository<CatalogItem, int>
{
    Task<IEnumerable<CatalogItem>> FindByNameAsync(string name);
}
