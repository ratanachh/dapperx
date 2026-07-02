using Dapper.Npa.Core.Attributes;

namespace Dapper.Npa.Tests.Fixtures;

[Embeddable]
public class ProductImage
{
    [Column]
    public string Url { get; set; } = string.Empty;

    [Column]
    public string Caption { get; set; } = string.Empty;
}
