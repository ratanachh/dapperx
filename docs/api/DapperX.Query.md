---
uid: DapperX.Query
summary: *content
---
The runtime engine behind the fluent [`IQuery<T>`](xref:DapperX.Abstractions.Query.IQuery`1) API: translating
LINQ-style expressions into SQL, materializing projections, applying global filters, and building parameters
safely.

- [`DapperX.Query.Query`](xref:DapperX.Query.Query) — `QueryBuilder<T>`, `QueryBuilderState<T>`, `QueryBuilderStateSnapshot`
- [`DapperX.Query.Expressions`](xref:DapperX.Query.Expressions) — `ExpressionParser`, `WhereTranslator`, `OrderByTranslator`
- [`DapperX.Query.Projections`](xref:DapperX.Query.Projections) — `ProjectionMaterializer`
- [`DapperX.Query.Filters`](xref:DapperX.Query.Filters) — `SoftDeleteBypassSelector`
- [`DapperX.Query.Sql`](xref:DapperX.Query.Sql) — `SqlParameterBuilder`
