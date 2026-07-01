namespace DapperX.Core.Models;
public sealed class EmbeddedMetadata
{
    public string PropertyName { get; init; } = string.Empty;
    public string EmbeddableType { get; init; } = string.Empty;
    public IReadOnlyList<AttributeOverrideEntry> Overrides { get; init; } = [];
}
public sealed class AttributeOverrideEntry
{
    public string Property { get; init; } = string.Empty;
    public string Column { get; init; } = string.Empty;
}
