using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

namespace DapperX.Tests.Fixtures;

[Entity]
[Table("users")]
public class DualAddressUser
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [Embedded]
    public Address BillingAddress { get; set; } = new();

    [Embedded]
    [AttributeOverride("City", "shipping_city")]
    [AttributeOverride("Country", "shipping_country")]
    public Address ShippingAddress { get; set; } = new();
}

[Repository]
public interface IDualAddressUserRepository : IRepository<DualAddressUser, int>
{
}
