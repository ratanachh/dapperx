using Dapper.Npa.Core.Attributes;

namespace Dapper.Npa.Tests.Fixtures;

[Embeddable]
public class Address
{
    [Column]
    public string City { get; set; } = string.Empty;

    [Column]
    public string Country { get; set; } = string.Empty;
}
