# Dapper NpaSample Application

Runnable ASP.NET Core minimal API demonstrating Dapper Npacompile-time repositories, derived queries, `IQuery`, global filters, soft delete, tenancy, auditing, secondary tables, batch/graph insert, locking, and paging.

## Run (default — Docker Compose SQL Server)

1. Start SQL Server: `docker compose -f samples/Dapper.NpaSampleApp/docker-compose.yml up -d`
2. Wait until the container is healthy (`docker compose ps` shows `healthy`).
3. `dotnet run --project samples/Dapper.NpaSampleApp/Dapper.NpaSampleApp.csproj`
4. Open `GET /` for route index.

Connection string in `appsettings.json` targets `localhost:14333` (mapped in `docker-compose.yml` to avoid clashing with a local SQL Server on 1433).

## Smoke test (all endpoints)

Prerequisites: `curl`, `jq`, app running at `http://localhost:5000`.

```bash
./samples/Dapper.NpaSampleApp/smoke-test.sh
```

Optional: `BASE_URL=http://localhost:5000 RESPONSES_FILE=/tmp/responses.txt ./samples/Dapper.NpaSampleApp/smoke-test.sh`

The script curls every `/demo/*` route in dependency order, asserts HTTP status codes, and writes full responses to `responses.txt`.

## SQLite (no Docker)

Set `DapperX:DatabaseProvider` to `Sqlite` in `appsettings.json` or environment. The app uses in-memory SQLite and recreates schema on startup.

## Feature map

| Route prefix | Demonstrates |
|---|---|
| `/demo/catalog` | CRUD, `FindAllById`, `ExistsById`, `Count`, `DeleteAllById`, `GetAll`/`Page`/`Slice`, derived queries, batch insert, `IQuery` locks, column transformer |
| `/demo/users` | Mapped superclass auditing, soft delete, tenancy, global filter, entity listeners, profile PK |
| `/demo/members` | `[SecondaryTable]` split persistence |
| `/demo/orders` | Lifecycle hooks, formula, generated column, relationships |
| `/demo/org` | Department / employee data |
| `/demo/graph` | `InsertGraphAsync` with `LazyCollection.Set` |
| `/demo/sqlite` | Provider switch notes |

CPQL and `[Immutable]` product variant are covered in `tests/Dapper.NpaTests` until generator edge cases are resolved for those shapes in the sample assembly.
