# Demo App Walkthrough

[`samples/DapperX.SampleApp`](https://github.com/ratanachh/dapperx/tree/main/samples/DapperX.SampleApp) is a
runnable ASP.NET Core minimal API that exercises nearly every DapperX feature end-to-end: CRUD, derived
queries, the fluent `IQuery<T>` API, global filters, soft delete, multi-tenancy, auditing, secondary tables,
batch/graph insert, row locking, and paging.

## Running it

**Default — Docker Compose SQL Server:**

```bash
docker compose -f samples/DapperX.SampleApp/docker-compose.yml up -d
# wait until `docker compose ps` shows the container healthy
dotnet run --project samples/DapperX.SampleApp/DapperX.SampleApp.csproj
```

The connection string in `appsettings.json` targets `localhost:14333` (mapped in `docker-compose.yml` to
avoid clashing with a local SQL Server on 1433). Open `GET /` for a route index.

**Provider configuration:**

This sample app is compiled for SQL Server via `DapperXDatabaseProvider` in
`samples/DapperX.SampleApp/DapperX.SampleApp.csproj`. The generator emits
`DapperX.Generated.DapperXConnectionFactory`, which the app uses for the provider-specific connection and
`ProviderName`. Switching to PostgreSql, MySql, or Sqlite means changing the compile-time property and
supplying the matching connection string entry.

## Smoke test

With the app running at `http://localhost:5000`:

```bash
./samples/DapperX.SampleApp/smoke-test.sh
```

Requires `curl` and `jq`. It calls every `/demo/*` route in dependency order, asserts HTTP status codes, and
writes full responses to `responses.txt`. Override the target with
`BASE_URL=http://localhost:5000 RESPONSES_FILE=/tmp/responses.txt ./samples/DapperX.SampleApp/smoke-test.sh`.

## Feature map

| Route prefix | Demonstrates |
|---|---|
| `/demo/catalog` | CRUD, `FindAllById`, `ExistsById`, `Count`, `DeleteAllById`, `GetAll`/`Page`/`Slice`, derived queries, batch insert, `IQuery` locks, column transformer |
| `/demo/users` | Mapped superclass auditing, soft delete, tenancy, global filter, entity listeners, profile PK |
| `/demo/members` | `[SecondaryTable]` split persistence |
| `/demo/orders` | Lifecycle hooks, formula, generated column, relationships |
| `/demo/org` | Department / employee data (`LazyMap` keyed relations) |
| `/demo/graph` | `InsertGraphAsync` with `LazyCollection.Set` |
| `/demo/provider` | Compile-time provider notes |

CPQL and the `[Immutable]` product variant are covered in `tests/DapperX.Tests` until generator edge cases are
resolved for those shapes in the sample assembly — see [CPQL](features/cpql.md) for examples pulled from
those tests.

## Where to look in the source

- `Entities/` — every mapped entity used by the demo routes (`CatalogProduct`, `AppUser`, `Member`,
  `SalesOrder`/`SalesOrderLine`, `Department`/`Employee`, `GraphParent`/`GraphChild`).
- `Repositories/` — the `[Repository]` interfaces, including the derived-query methods on
  `ICatalogProductRepository`.
- `DemoEndpoints.cs` — the minimal-API route handlers calling into each repository.
- `Infrastructure/` — `SampleAuditingProvider`, `SampleTenantProvider`, and `SampleAuditListener`, the
  pluggable providers behind `[CreatedBy]`/`[LastModifiedBy]`/`[TenantId]`/`[EntityListeners]`.
