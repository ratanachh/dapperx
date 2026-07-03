using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("customers")]
public class Customer
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Name { get; set; } = string.Empty;
}
