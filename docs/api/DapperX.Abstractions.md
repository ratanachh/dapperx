---
uid: DapperX.Abstractions
summary: *content
---
The contracts consumers work against directly. Everything here is an interface or plain data type —
independent of how the runtime or source generator implements it — covering repositories, paging, sorting,
querying, value conversion, auditing, multi-tenancy, and the exceptions DapperX throws.

- [`DapperX.Abstractions.Repositories`](xref:DapperX.Abstractions.Repositories) — `IRepository<T, TId>`, the core generated-repository contract
- [`DapperX.Abstractions.Query`](xref:DapperX.Abstractions.Query) — `IQuery<T>`, the fluent runtime query API
- [`DapperX.Abstractions.Paging`](xref:DapperX.Abstractions.Paging) — `Page<T>`, `Slice<T>`, `Pageable`
- [`DapperX.Abstractions.Sorting`](xref:DapperX.Abstractions.Sorting) — `Sort`
- [`DapperX.Abstractions.Converters`](xref:DapperX.Abstractions.Converters) — `IValueConverter<TProperty, TColumn>`
- [`DapperX.Abstractions.Auditing`](xref:DapperX.Abstractions.Auditing) — `IAuditingProvider`
- [`DapperX.Abstractions.Tenancy`](xref:DapperX.Abstractions.Tenancy) — `ITenantProvider`
- [`DapperX.Abstractions.Sequences`](xref:DapperX.Abstractions.Sequences) — `ISequenceAllocator`
- [`DapperX.Abstractions.StoredProcedures`](xref:DapperX.Abstractions.StoredProcedures) — stored-procedure parameter/result types
- [`DapperX.Abstractions.Configuration`](xref:DapperX.Abstractions.Configuration) — `IDapperXOptions`
- [`DapperX.Abstractions.Logging`](xref:DapperX.Abstractions.Logging) — `DapperXLogEntry`
- [`DapperX.Abstractions.Graphs`](xref:DapperX.Abstractions.Graphs) — `InvalidEntityGraphException`
- [`DapperX.Abstractions.Lifecycle`](xref:DapperX.Abstractions.Lifecycle) — `ILifecycleInvoker<T>`
- [`DapperX.Abstractions.Exceptions`](xref:DapperX.Abstractions.Exceptions) — `ConcurrencyException`, `MappingException`, and friends
