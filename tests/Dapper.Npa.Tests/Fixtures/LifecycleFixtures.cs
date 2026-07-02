using Dapper.Npa.Abstractions.Repositories;
using Dapper.Npa.Core.Attributes;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Tests.Fixtures;

public sealed class AuditListener
{
    public static int PrePersistCount { get; private set; }
    public static int PostLoadCount { get; private set; }

    public static void Reset()
    {
        PrePersistCount = 0;
        PostLoadCount = 0;
    }

    [PrePersist]
    public void BeforeInsert(object entity) => PrePersistCount++;

    [PostLoad]
    public void AfterLoad(object entity) => PostLoadCount++;
}

[Entity]
[Table("listener_only_items")]
[EntityListeners(typeof(AuditListener))]
public class ListenerOnlyItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;
}

[Repository]
public interface IListenerOnlyItemRepository : IRepository<ListenerOnlyItem, int>
{
}

[Entity]
[Table("shared_listener_items")]
[EntityListeners(typeof(AuditListener))]
public class SharedListenerItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;
}

[Repository]
public interface ISharedListenerItemRepository : IRepository<SharedListenerItem, int>
{
}

[Entity]
[Table("batch_lifecycle_items")]
public class BatchLifecycleItem
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [PrePersist]
    public void OnPrePersist() { }

    [PrePersistBatch]
    public void OnPrePersistBatch() { }

    [PostPersistBatch]
    public void OnPostPersistBatch() { }

    [PreUpdateBatch]
    public void OnPreUpdateBatch() { }

    [PostUpdateBatch]
    public void OnPostUpdateBatch() { }

    [PreRemoveBatch]
    public void OnPreRemoveBatch() { }

    [PostRemoveBatch]
    public void OnPostRemoveBatch() { }
}

[Repository]
public interface IBatchLifecycleItemRepository : IRepository<BatchLifecycleItem, int>
{
}
