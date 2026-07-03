namespace DapperX.Batching.Graph;
public static class TopologicalSorter
{
    public static IReadOnlyList<string> Sort(IReadOnlyDictionary<string, IReadOnlyList<string>> dependencies)
    {
        var visited = new HashSet<string>();
        var result = new List<string>();
        foreach (var node in dependencies.Keys)
            Visit(node, dependencies, visited, result);
        return result;
    }
    private static void Visit(string node, IReadOnlyDictionary<string, IReadOnlyList<string>> deps, HashSet<string> visited, List<string> result)
    {
        if (!visited.Add(node)) return;
        if (deps.TryGetValue(node, out var children))
            foreach (var child in children) Visit(child, deps, visited, result);
        result.Add(node);
    }
}
