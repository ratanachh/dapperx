# Batch & Graph Operations

## Batch operations

`IRepository<T, TId>` provides chunked batch variants of insert/update/delete/upsert:

```csharp
await repo.InsertManyAsync(items);                 // POST /demo/catalog/batch
await repo.UpdateManyAsync(items);
await repo.DeleteManyAsync(items);
await repo.UpsertManyAsync(items);
```

Each is chunked into groups of `batchSize` (defaults to
[`DapperXOptions.BatchSize`](xref:DapperX.Runtime.Configuration.DapperXOptions.BatchSize), 1000). For inserts,
once the batch exceeds `bulkThreshold` (default
[`DapperXOptions.BulkThreshold`](xref:DapperX.Runtime.Configuration.DapperXOptions.BulkThreshold), 5000 rows),
DapperX switches to a provider-specific bulk insert path (e.g. SQL Server's `SqlBulkCopy`) instead of chunked
`INSERT` statements — see [Providers](providers.md).

`UpsertManyAsync` is likewise chunked, but it executes each entity in the chunk individually rather than using a provider-specific bulk upsert path.

## Graph operations

`InsertGraphAsync`/`UpdateGraphAsync`/`DeleteGraphAsync` persist a root entity together with its related
entities, walking `Cascade`-enabled relations and executing statements in dependency order (parents before
children on insert/update, children before parents on delete) so foreign key constraints are always satisfied.

```csharp
[Entity]
[Table("sample_graph_parents")]
public class GraphParent
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(GraphChild.Parent), Cascade = CascadeType.All)]
    public LazyCollection<GraphChild> Children { get; set; } = new();
}
```

```csharp
var parent = new GraphParent { Name = "Region A" };
parent.Children.Set(labels.Select(label => new GraphChild { Label = label }).ToList());

await repo.InsertGraphAsync(parent);
```

`LazyCollection<T>.Set(...)` replaces the collection's contents outright (as opposed to loading it lazily from
the database) — this is how you attach new children to a not-yet-persisted parent before calling
`InsertGraphAsync`. The generator resolves the correct insert order across the whole graph, even for
multi-level hierarchies, using a dependency graph built from `Cascade`/`MappedBy` metadata at compile time.

## Transactions

Both batch and graph methods accept an optional `IDbTransaction`. To run several repository calls atomically,
use [`WithTransactionAsync`](xref:DapperX.Abstractions.Repositories.IRepository`2.WithTransactionAsync*) on any
repository and pass the transaction it opens into each subsequent call.
