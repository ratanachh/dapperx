namespace DapperX.Generator.Models;

internal sealed class ExecutionPlanModel
{
    public string PlanName { get; init; } = string.Empty;
    public IReadOnlyList<ExecutionNodeModel> Nodes { get; init; } = [];
}

internal sealed class ExecutionNodeModel
{
    public string EntityTypeName { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public int Level { get; init; }
    public string? ForeignKeyProperty { get; init; }
    public string? ParentEntityTypeName { get; init; }
    public string? RelationshipProperty { get; init; }
    public string? JoinTable { get; init; }
}
