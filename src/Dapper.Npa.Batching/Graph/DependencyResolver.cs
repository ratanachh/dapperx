namespace Dapper.Npa.Batching.Graph;
public static class DependencyResolver
{
    public static bool HasCycle(IReadOnlyDictionary<string, IReadOnlyList<string>> graph)
    {
        var visited = new HashSet<string>();
        var inStack = new HashSet<string>();
        bool DFS(string node) {
            if (inStack.Contains(node)) return true;
            if (!visited.Add(node)) return false;
            inStack.Add(node);
            if (graph.TryGetValue(node, out var neighbors))
                foreach (var n in neighbors) if (DFS(n)) return true;
            inStack.Remove(node);
            return false;
        }
        return graph.Keys.Any(DFS);
    }
}
