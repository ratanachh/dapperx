using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

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
