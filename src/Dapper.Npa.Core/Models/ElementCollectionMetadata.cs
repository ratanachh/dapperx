namespace Dapper.Npa.Core.Models;
public sealed class ElementCollectionMetadata
{
    public string PropertyName { get; init; } = string.Empty;
    public string CollectionTable { get; init; } = string.Empty;
    public string JoinColumn { get; init; } = string.Empty;
    public string? ValueColumn { get; init; }
    public string ElementTypeName { get; init; } = string.Empty;
}
