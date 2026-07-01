using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

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
