using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("ct_products")]
public class ColumnTransformerProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [ColumnTransformer(Read = "name", Write = "?")]
    public string DisplayName { get; set; } = string.Empty;

    [ColumnTransformer(Read = "UPPER(name)")]
    public string UpperName { get; set; } = string.Empty;
}

[Repository]
public interface IColumnTransformerProductRepository : IRepository<ColumnTransformerProduct, int>
{
}
