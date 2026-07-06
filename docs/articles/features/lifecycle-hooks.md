# Lifecycle Hooks

Mark a method on an entity with a lifecycle attribute and DapperX's generated repository calls it at the
matching point in the entity's insert/update/delete/load lifecycle — no base class or interface required:

```csharp
[Entity]
[Table("sample_orders")]
public class SalesOrder
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Code { get; set; } = string.Empty;

    [PrePersist]
    public void OnPrePersist() { /* e.g. normalize Code, stamp defaults */ }

    [PostPersist]
    public void OnPostPersist() { /* e.g. fire a domain event now that Id is assigned */ }
}
```

## Available hooks

| Attribute | Fires |
|---|---|
| [`[PrePersist]`](xref:DapperX.Core.Attributes.PrePersistAttribute) | Immediately before insert |
| [`[PostPersist]`](xref:DapperX.Core.Attributes.PostPersistAttribute) | Immediately after insert |
| [`[PreUpdate]`](xref:DapperX.Core.Attributes.PreUpdateAttribute) | Immediately before update |
| [`[PostUpdate]`](xref:DapperX.Core.Attributes.PostUpdateAttribute) | Immediately after update |
| [`[PreRemove]`](xref:DapperX.Core.Attributes.PreRemoveAttribute) | Immediately before delete (including soft delete) |
| [`[PostRemove]`](xref:DapperX.Core.Attributes.PostRemoveAttribute) | Immediately after delete |
| [`[PostLoad]`](xref:DapperX.Core.Attributes.PostLoadAttribute) | Immediately after an entity is materialized from a query result |

Each also has a `...Batch` counterpart (`PrePersistBatchAttribute`, `PostPersistBatchAttribute`, etc.) invoked
once per batch rather than once per entity, for hooks cheap enough to skip per-row overhead on
`InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync`.

## External listeners

Instead of methods on the entity itself, attach one or more external listener classes with
`[EntityListeners(typeof(MyListener))]` — useful for cross-cutting concerns (metrics, auditing, test
instrumentation) that shouldn't live inside the entity. The sample app's `SampleAuditListener` uses this to
count `PrePersist` calls for `AppUser` without touching the entity class.
