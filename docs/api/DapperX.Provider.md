---
uid: DapperX.Provider
summary: *content
---
The database-specific pieces DapperX swaps in based on which provider you're targeting: SQL dialect
(quoting, paging syntax, upsert syntax) and bulk-insert execution.

- [`DapperX.Provider.Common`](xref:DapperX.Provider.Common) — `IDatabaseProvider`, `DatabaseProviderFactory`, `DatabaseProviderBase`, `SqlDialect`
- [`DapperX.Provider.SqlServer`](xref:DapperX.Provider.SqlServer) — SQL Server dialect and bulk executor
- [`DapperX.Provider.PostgreSql`](xref:DapperX.Provider.PostgreSql) — PostgreSQL dialect and bulk executor
- [`DapperX.Provider.MySql`](xref:DapperX.Provider.MySql) — MySQL dialect and batch executor
- [`DapperX.Provider.Sqlite`](xref:DapperX.Provider.Sqlite) — SQLite dialect and provider
