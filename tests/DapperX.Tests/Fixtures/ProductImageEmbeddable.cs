using DapperX.Core.Attributes;

namespace DapperX.Tests.Fixtures;

[Embeddable]
public class ProductImage
{
    [Column]
    public string Url { get; set; } = string.Empty;

    [Column]
    public string Caption { get; set; } = string.Empty;
}
