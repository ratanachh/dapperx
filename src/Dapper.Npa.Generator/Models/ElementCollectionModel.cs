namespace Dapper.Npa.Generator.Models;

internal sealed class ElementCollectionModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string CollectionTable { get; init; } = string.Empty;
    public string JoinColumn { get; init; } = string.Empty;
    public string ElementTypeName { get; init; } = string.Empty;
    public bool IsEmbeddable { get; init; }
    public IReadOnlyList<string> ValueColumns { get; init; } = [];
    /// <summary>Embeddable element property names; parallel to <see cref="ValueColumns"/>.</summary>
    public IReadOnlyList<string> ValuePropertyNames { get; init; } = [];
    public string? OrderColumnName { get; init; }
}
