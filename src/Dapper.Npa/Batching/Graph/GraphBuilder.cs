namespace Dapper.Npa.Batching.Graph;
public sealed class GraphBuilderResult
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Adjacency { get; init; } = new Dictionary<string, IReadOnlyList<string>>();
    public bool HasCycles { get; init; }
}
