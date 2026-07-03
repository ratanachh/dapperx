namespace DapperX.Generator.Models;

internal enum LifecycleKind
{
    PrePersist,
    PostPersist,
    PreUpdate,
    PostUpdate,
    PreRemove,
    PostRemove,
    PostLoad,
    PrePersistBatch,
    PostPersistBatch,
    PreUpdateBatch,
    PostUpdateBatch,
    PreRemoveBatch,
    PostRemoveBatch,
}

internal sealed class LifecycleMethodModel
{
    public string MethodName { get; init; } = string.Empty;
    public LifecycleKind Kind { get; init; }
}
