# Providers

DapperX supports four database providers out of the box, each with its own SQL dialect and bulk-insert
strategy, under `DapperX.Provider.{SqlServer,PostgreSql,MySql,Sqlite}`:

- **SQL Server** — dialect quoting/paging syntax in `SqlServerDialect`; bulk inserts via `SqlServerBulkExecutor`
  (`SqlBulkCopy`).
- **PostgreSQL** — `PostgreSqlDialect`; bulk inserts via `PostgreSqlBulkExecutor` (`COPY`).
- **MySQL** — `MySqlDialect`; batched multi-row inserts via `MySqlBatchExecutor`.
- **SQLite** — `SqliteDialect`, used by the sample app's no-Docker mode.

Provider selection is a **runtime** concern — the same compiled entities/repositories work against any
supported provider; only the connection you hand to `AddDapperXRepositories` (and, for a handful of
provider-aware bulk-insert code paths, the resolved `IDatabaseProvider`) determines which dialect executes.
There's no separate build per provider.

## Choosing a provider

The sample app's `SampleDatabaseHost` shows the common pattern: read a configuration flag, and create the
matching `IDbConnection`:

```csharp
public IDbConnection CreateConnection() =>
    Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
        ? new SqliteConnection(ConnectionString)
        : new SqlConnection(ConnectionString);
```

```csharp
builder.Services.AddDapperXRepositories(_ => CreateOpenConnection(connectionString));
```

Whatever `IDbConnection` implementation you open determines the provider DapperX talks to underneath
`IRepository<T, TId>`/`IQuery<T>` — swapping providers (e.g. SQL Server in production, SQLite in local
dev/tests) is a matter of swapping the connection factory and, for the sample app,
`DapperX:DatabaseProvider`/`ConnectionStrings:Sqlite`/`ConnectionStrings:Default` in `appsettings.json`.

## Dialect differences DapperX handles for you

Compile-time SQL generation accounts for each provider's parameter placeholder syntax, identifier quoting,
`LIMIT`/`OFFSET` vs. `OFFSET`/`FETCH` paging, identity/auto-increment vs. sequence-based ID generation
(`[GeneratedValue]`), and upsert syntax (`MERGE`/`ON CONFLICT`/`ON DUPLICATE KEY`) — you write one set of
attributes and get correct SQL for whichever provider is configured.

## Provider-specific integration tests

Each provider has its own integration test project —
`tests/DapperX.IntegrationTests.{SqlServer,PostgreSql,MySql,Sqlite}` — run against real (Testcontainers-backed,
for the server databases) instances in CI, covering the dialect and bulk-insert paths listed above.
