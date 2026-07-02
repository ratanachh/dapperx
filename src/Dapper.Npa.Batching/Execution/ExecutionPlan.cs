namespace Dapper.Npa.Batching.Execution;
public sealed class ExecutionPlan
{
    public IReadOnlyList<ExecutionNode> Nodes { get; init; } = [];
}
public sealed class ExecutionNode
{
    public string EntityTypeName { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty; // Insert, Update, Delete
    public int Level { get; init; }
    public string? ForeignKeyProperty { get; init; }
    public string? ParentEntityTypeName { get; init; }
}
