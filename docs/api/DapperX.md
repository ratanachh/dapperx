---
uid: DapperX
summary: *content
---
Root namespace for the DapperX library. There are no types directly in `DapperX` itself — everything public
lives under one of the sub-namespaces below.

- [`DapperX.Abstractions`](xref:DapperX.Abstractions) — the contracts consumers work against (repositories, paging, sorting, query)
- [`DapperX.Core`](xref:DapperX.Core) — the mapping attributes and metadata the source generator reads
- [`DapperX.Runtime`](xref:DapperX.Runtime) — services backing the generated repositories at runtime
- [`DapperX.Batching`](xref:DapperX.Batching) — batch chunking and dependency-ordered execution
- [`DapperX.Relations`](xref:DapperX.Relations) — lazy loading for `[OneToOne]`/`[OneToMany]`/`[ManyToOne]`/`[ManyToMany]`
- [`DapperX.Lifecycle`](xref:DapperX.Lifecycle) — `[PrePersist]`/`[PostLoad]`-style hook invocation
- [`DapperX.Query`](xref:DapperX.Query) — the fluent `IQuery<T>` runtime engine
- [`DapperX.Provider`](xref:DapperX.Provider) — SQL Server, PostgreSQL, MySQL, and SQLite specifics
