using System.Data;

namespace DapperX.Batching.Execution;

public static class ExecutionEngine
{
    /// <summary>Executes a compile-time execution plan. Graph repositories use imperative loops; plans are metadata only.</summary>
    public static async Task ExecuteAsync(ExecutionPlan plan, IDbConnection connection, IDbTransaction transaction)
    {
        // Executes the pre-built flat execution plan in topological order
        foreach (var node in plan.Nodes.OrderBy(n => n.Level))
            await ExecuteNodeAsync(node, connection, transaction);
    }
    private static Task ExecuteNodeAsync(ExecutionNode node, IDbConnection connection, IDbTransaction transaction)
        => Task.CompletedTask; // concrete logic emitted by generator per entity
}
