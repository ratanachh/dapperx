namespace DapperX.Core.Models;
public sealed class NamedEntityGraphMetadata
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<string> AttributeNodes { get; init; } = [];
    public IReadOnlyList<SubGraphEntry> SubGraphs { get; init; } = [];
}
public sealed class SubGraphEntry
{
    public string RelationshipProperty { get; init; } = string.Empty;
    public IReadOnlyList<string> AttributeNodes { get; init; } = [];
}
