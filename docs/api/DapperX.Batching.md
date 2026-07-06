---
uid: DapperX.Batching
summary: *content
---
The machinery behind `InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync` and the entity-graph operations:
splitting a batch into chunks, and figuring out the order entities must be written in when a graph has
dependencies between them.

- [`DapperX.Batching.Batch`](xref:DapperX.Batching.Batch) — `BatchChunker`, `BatchExecutor`
- [`DapperX.Batching.Graph`](xref:DapperX.Batching.Graph) — `DependencyResolver`, `TopologicalSorter`, `GraphBuilderResult`
- [`DapperX.Batching.Execution`](xref:DapperX.Batching.Execution) — `ExecutionPlan`, `ExecutionNode`, `ExecutionEngine`
