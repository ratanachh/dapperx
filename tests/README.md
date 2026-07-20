# DapperX tests

## Compile-time provider matrix

Generator SQL is selected at **compile time** via the `DapperXDatabaseProvider` MSBuild property (one dialect per test assembly).

| Project | Provider | Role |
|---|---|---|
| `DapperX.Tests` | SqlServer | Primary unit tests + Roslyn/diagnostic validation (`single-project`) |
| `DapperX.Tests.PostgreSql` | PostgreSql | Matrix-4 generation tests (linked from `DapperX.Tests.Shared`) |
| `DapperX.Tests.MySql` | MySql | Matrix-4 generation tests |
| `DapperX.Tests.Sqlite` | Sqlite | Matrix-4 + sqlite-only Diagnostic tests |

Shared sources: `DapperX.Tests.Shared/` (`ProviderExpectations`, `GeneratedSourceReader`, `ProviderGenerationTests.props`).

## Integration tests (Testcontainers)

| Project | Database | Notes |
|---|---|---|
| `DapperX.IntegrationTests.SqlServer` | SQL Server | Docker required |
| `DapperX.IntegrationTests.PostgreSql` | PostgreSQL | Docker required |
| `DapperX.IntegrationTests.MySql` | MySQL | Docker required |
| `DapperX.IntegrationTests.Sqlite` | SQLite | In-memory (no Docker) |

Shared: `DapperX.IntegrationTests.Shared` — `SqlExecutionCountFixture`, `IntegrationFixtures.props`, `IntegrationScenarios.props`, `DatabaseBootstrap`, `IntegrationProcedureBootstrap`, `IntegrationEnvironment`, scenarios (`IntegAllProviders`, `IntegAdvancedFeatures`, `IntegStoredProcedure` and `IntegLockTimeout` exclude Sqlite compile, `IntegSqliteDialect`).

### Provider-specific integration harness notes

| Provider | Harness detail |
|---|---|
| **PostgreSql** | Generated SQL uses `column = ANY(@param)` for IN lists and `is_deleted = false` (not `= 0`) on boolean columns. Stored procedures with OUT params: `CALL proc(@in, @inout, NULL, …)` — OUT-only slots are `NULL` in SQL; values via `DynamicParameters` direction. Lock timeout: `SET lock_timeout = {ms}` as a separate `ExecuteAsync` before the SELECT. |
| **MySql** | Bulk insert (`MySqlBulkCopy`) requires server `--local-infile=1` on the Testcontainers image **and** client `AllowLoadLocalInfile=true` in the connection string (`IntegrationEnvironment`). Stored procedures with OUT/InOut: `CommandType.StoredProcedure` + procedure name only; parameter names in `IntegrationProcedureBootstrap` match C# (`orderId`, `total`, …). |
| **SqlServer** | Stored procedures: procedure name + `CommandType.StoredProcedure`. Lock timeout: `SET LOCK_TIMEOUT {ms}` preamble (separate call). |
| **Sqlite** | No Docker; `SqliteGuidTypeHandler` for `TEXT` tenant columns. Raw SQL in scenarios uses `is_deleted = 0` where applicable (not boolean type). |

### Matrix-4 provider SQL assertions

Use `ProviderExpectations.AssertInClause` and `AssertBooleanFilterLiteral` in shared generation tests — do not hardcode `IN @ids` or `is_deleted = 0` when the test project may compile as PostgreSql.

## Commands

```bash
# All compile-time unit + matrix tests (no Docker)
dotnet test DapperX.slnx --filter "FullyQualifiedName!~IntegrationTests"

# Integration tests (Docker required for SqlServer/PostgreSql/MySql)
dotnet test DapperX.slnx --filter "FullyQualifiedName~IntegrationTests"
```

## Feature coverage legend

See `Tasks.md` EPIC 26a: `single-project`, `matrix-4`, `sqlite-only`, `integration-{Provider}`.

**Single-project (SqlServer):** Roslyn mapping/CPQL/parser validation stays in [`DapperX.Tests`](DapperX.Tests/) only — not linked into the provider matrix assemblies. See `SqlServerRegexDiagnosticTests` and test file headers in that project.

**Integration:** Shared scenarios link via `DapperX.IntegrationTests.Shared/IntegrationScenarios.props` into each `DapperX.IntegrationTests.{Provider}` project. EPIC 26 runtime extras: `IntegRuntimeExtrasTests` (global filter, column transformer, `IQuery` projection/split, pessimistic read). Sqlite registers `SqliteGuidTypeHandler` for `TEXT` tenant columns; SqlServer DDL uses plain `INT PRIMARY KEY` for `Assigned`-id fixtures (not `IDENTITY`).

### Smoke-test regression (sample app fixes → automated tests)

| Fix | Test coverage |
|---|---|
| Identity INSERT omits `@Id` for identity columns | `IdentityInsertRegressionTests`, `GeneratedColumnGenerationTests`, `GeneratedValueMatrixTests` |
| SqlServer `ORDER BY` before `OFFSET/FETCH` | `GetAllAsyncGenerationTests`, `SlicePagingMatrixTests`, `PagePagingMatrixTests` |
| Sorted paging — no duplicate `ORDER BY` | `PagedSortSqlTests` |
| Global filter read params (`BuildReadParameters`) | `GlobalFilterGenerationTests`, `GlobalFilterMatrixTests` (`TenantRegionUser` / `MatrixTenantRegionUser`) |
| Tenancy `DeleteById` with global filters | `GlobalFilterGenerationTests` (`TenantRegionUserRepositoryImpl`) |
| SqlServer lock hints after `FROM` table | `SqlServerTableHintTests`, `LockingGenerationTests`, `LockingMatrixTests`, `ConcurrencyAndLockingTests` |
| Integration: parameterized filter, tenant soft-delete, sorted paging | `IntegRuntimeExtrasTests` (`IntegTenantRegionUser`) |

## Performance verification (EPIC 26b)

| Project | Role |
|---|---|
| `DapperX.Performance.Tests` | Performance guarantee compile-time checks (`PerformanceRequirementsTests`); optional BenchmarkDotNet `ResolveColumnBenchmark` |

```bash
dotnet test tests/DapperX.Performance.Tests/DapperX.Performance.Tests.csproj
```

BenchmarkDotNet micro-benchmarks are not run in CI by default.

## Sample application (EPIC 27)

[`samples/DapperX.SqlServer.SampleApp/README.md`](../samples/DapperX.SqlServer.SampleApp/README.md) — full feature demo app; `docker compose -f samples/DapperX.SqlServer.SampleApp/docker-compose.yml up -d` then `dotnet run --project samples/DapperX.SqlServer.SampleApp/DapperX.SqlServer.SampleApp.csproj`; endpoint smoke test: `./samples/DapperX.SqlServer.SampleApp/smoke-test.sh`
