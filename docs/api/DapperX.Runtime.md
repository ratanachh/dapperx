---
uid: DapperX.Runtime
summary: *content
---
The services that back every generated repository at runtime: options binding, value conversion, SQL
execution and logging, and the base class the generator's repository implementations derive from.

- [`DapperX.Runtime.Repositories`](xref:DapperX.Runtime.Repositories) — `DapperXRepositoryBase<TEntity, TId>`, the base every generated repository extends
- [`DapperX.Runtime.Configuration`](xref:DapperX.Runtime.Configuration) — `DapperXOptions`
- [`DapperX.Runtime.Converters`](xref:DapperX.Runtime.Converters) — built-in `IValueConverter` implementations (enum, JSON, UTC date/time)
- [`DapperX.Runtime.Execution`](xref:DapperX.Runtime.Execution) — `DbExecutor`, `DbExecutionLogContext`
- [`DapperX.Runtime.Logging`](xref:DapperX.Runtime.Logging) — `SqlExecutionLogger`, `ExecutableSqlFormatter`, `ParameterExtractor`
- [`DapperX.Runtime.Query`](xref:DapperX.Runtime.Query) — `RepositoryQuery<T>`, `QueryRuntimeConfig`, `SqlServerTableHint`
- [`DapperX.Runtime.Utilities`](xref:DapperX.Runtime.Utilities) — `SqlHelper`, `TypeHelper`
