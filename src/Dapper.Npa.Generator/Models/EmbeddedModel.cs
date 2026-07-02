namespace Dapper.Npa.Generator.Models;

internal sealed class EmbeddedModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string EmbeddableTypeName { get; init; } = string.Empty;
    public string EmbeddableTypeFqn { get; init; } = string.Empty;
    public IReadOnlyList<(string Property, string Column)> Overrides { get; init; } = [];
    public IReadOnlyList<string> InnerPropertyNames { get; init; } = [];
}
