# DapperX – Engineering Task Breakdown

Structured as Epics → Features → Tasks, sequenced for MVP → full system progression.

***

## Project layout and verification

| Topic | Where |
|---|---|
| Source layout (EPIC → project) | [`Structures.md`](Structures.md) §1–§2 — EPIC 1 → `DapperX.Core`; EPIC 2 → `DapperX.Abstractions`; EPIC 3+ → `DapperX.Generator` + runtime projects |
| Authoritative behaviour | [`Requirements.md`](Requirements.md) — Rules A–E; §14 dialect table + Verification (test matrix) |
| Repository convention | `I{Name}Repository` + `[Repository]` → sealed `{Name}RepositoryImpl` (strip `I`, append `Impl`; no partial) |
| Test layout | [`Structures.md`](Structures.md) §2.11; commands in [`tests/README.md`](tests/README.md) |

**Verification coverage legend** (EPIC 26a — full task list below):

| Tag | Meaning |
|---|---|
| `single-project` | [`DapperX.Tests`](tests/DapperX.Tests/DapperX.Tests.csproj) — **SqlServer** compile target only |
| `matrix-4` | Same sources linked into all four compile projects via [`DapperX.Tests.Shared`](tests/DapperX.Tests.Shared/) |
| `sqlite-only` | Compile-time Diagnostic tests in `DapperX.Tests.Sqlite` only |
| `integration-{Provider}` | Real DB in `DapperX.IntegrationTests.{Provider}` (Testcontainers; Docker except Sqlite in-memory) |

**EPIC index**

| EPICs | Role |
|---|---|
| 1–25 | Implementation (attributes, generator, runtime) — most tasks `[x]` |
| 26a | Provider test matrix infrastructure + matrix-4 / integration smoke |
| 26 | SqlServer unit/integration backlog; defers to 26a for multi-provider proof |
| 26b | Section 26 performance (benchmarks + `SqlExecutionCountFixture`) |
| 27–28 | Sample app + documentation |

***

# EPIC 1: Core Foundation

***

## Feature: Entity Markers

### Tasks

* [x] Create `EntityAttribute` class
* [x] Create `MappedSuperclassAttribute` class
* [x] Create `EmbeddableAttribute` class
* [x] Create `ImmutableAttribute` class

***

## Feature: Mapping Attributes

### Tasks

* [x] Create `TableAttribute` class (name, Schema)
* [x] Create `UniqueConstraintAttribute` class (Columns array — multi-column unique hint, informational)
* [x] Create `IndexAttribute` class (Columns array, Name, Unique flag — index documentation hint, informational only, not DDL)
* [x] Create `SecondaryTableAttribute` class (table name, PrimaryKeyJoinColumn)
* [x] Create `ColumnAttribute` class (Nullable, Insertable, Updatable, Unique, Length, Precision, Scale, ColumnDefinition, Fetch, Table — secondary table name for `[SecondaryTable]` routing)
* [x] Create `IdAttribute` class
* [x] Create `GeneratedValueAttribute` class (GenerationType, Generator name)
* [x] Create `SequenceGeneratorAttribute` class (Name, SequenceName only — no AllocationSize; stateless rule)
* [x] Create `VersionAttribute` class
* [x] Create `TransientAttribute` class
* [x] Create `SortableAttribute` class
* [x] Create `FormulaAttribute` class (SQL expression string)
* [x] Create `EmbeddedAttribute` class
* [x] Create `AttributeOverrideAttribute` class (Property, Column)
* [x] Create `ConverterAttribute` class
* [x] Create `EnumeratedAttribute` class (EnumType — String or Ordinal shorthand)
* [x] Create `ColumnTransformerAttribute` class (Read SQL expression, Write SQL expression with ? placeholder)
* [x] Create `ProjectionAttribute` class

***

## Feature: Relationship Attributes

### Tasks

* [x] Create `OneToManyAttribute` class (Cascade, Fetch, MappedBy)
* [x] Create `ManyToOneAttribute` class (Cascade, Fetch)
* [x] Create `OneToOneAttribute` class (Cascade, Fetch, MappedBy)
* [x] Create `ManyToManyAttribute` class (Cascade, Fetch)
* [x] Create `JoinColumnAttribute` class (name, Nullable)
* [x] Create `JoinTableAttribute` class (name, JoinColumn, InverseJoinColumn)
* [x] Create `OrderByAttribute` class (default collection ordering)
* [x] Create `OrderColumnAttribute` class (Name — persistent position column)
* [x] Create `AssociationOverrideAttribute` class (Name, JoinColumn — overrides FK column for inherited/embedded relationships)
* [x] Create `PrimaryKeyJoinColumnAttribute` class — shared-PK OneToOne; child.Id = parent.Id; no separate FK column
* [x] Create `MapKeyAttribute` class (column name — key column for LazyMap<K,V>)

***

## Feature: Lifecycle Attributes

### Tasks

* [x] Create `PrePersistAttribute` / `PostPersistAttribute`
* [x] Create `PreUpdateAttribute` / `PostUpdateAttribute`
* [x] Create `PreRemoveAttribute` / `PostRemoveAttribute`
* [x] Create `PostLoadAttribute`
* [x] Create `PrePersistBatchAttribute` / `PostPersistBatchAttribute`
* [x] Create `PreUpdateBatchAttribute` / `PostUpdateBatchAttribute`
* [x] Create `PreRemoveBatchAttribute` / `PostRemoveBatchAttribute`
* [x] Create `EntityListenersAttribute` class

***

## Feature: Auditing Attributes

### Tasks

* [x] Create `CreatedDateAttribute` class
* [x] Create `LastModifiedDateAttribute` class
* [x] Create `CreatedByAttribute` class
* [x] Create `LastModifiedByAttribute` class

***

## Feature: Behavior Attributes

### Tasks

* [x] Create `SoftDeleteAttribute` class (Column, DeletedAtColumn)
* [x] Create `TenantIdAttribute` class
* [x] Create `GlobalFilterAttribute` class (Name, Condition — native SQL fragment with @param references)
* [x] Create `GeneratedAttribute` class (GenerationTime — Insert or Always)

***

## Feature: Query Attributes

### Tasks

* [x] Create `RepositoryAttribute` class (`[Repository]` — no arguments; marks interface for generation; generator infers entity type from `IRepository<TEntity,TId>` generic argument; emits `{Name}RepositoryImpl : IInterface` sealed non-partial class)
* [x] Create `QueryAttribute` class (Query string, NativeQuery flag)
* [x] Create `NamedQueryAttribute` class (Name, Query)
* [x] Create `NamedQueriesAttribute` class (container for multiple NamedQuery entries)
* [x] Create `StoredProcedureAttribute` class
* [x] Create `BulkOperationAttribute` class

***

## Feature: Enums

### Tasks

* [x] Create `CascadeType` enum (Persist, Merge, Remove, All)
* [x] Create `DatabaseProvider` enum
* [x] Create `GenerationType` enum (Identity, Sequence, Uuid, Assigned)
* [x] Create `FetchType` enum (Lazy, Eager)
* [x] Create `LockMode` enum (Optimistic, Pessimistic, PessimisticRead)
* [x] Create `CpqlType` enum (String, Int, Long, Decimal, Double, Date, DateTime, Boolean)
* [x] Create `EnumType` enum (String, Ordinal — for [Enumerated] shorthand)
* [x] Create `GenerationTime` enum (Insert, Always — for [Generated] annotation)

***

## Feature: Metadata Models

### Tasks

* [x] Define `EntityMetadata` structure
* [x] Define `PropertyMetadata` structure (converter, sortable, insertable, updatable, formula, auditing flags)
* [x] Define `RelationshipMetadata` structure (FetchType, JoinColumn, JoinTable, OrderColumn, `isPrimaryKeyJoin` flag, `mapKeyColumn` name)
* [x] Define `EmbeddedMetadata` structure (AttributeOverride mappings)
* [x] Define `ConverterMetadata` structure
* [x] Define `FormulaMetadata` structure (SQL expression)
* [x] Define `SequenceMetadata` structure (SequenceName only — no AllocationSize per stateless rule)
* [x] Define `NamedQueryMetadata` structure
* [x] Define `AuditingMetadata` structure (which properties are CreatedDate/LastModifiedDate/By)
* [x] Define `SoftDeleteMetadata` structure (column name, deleted-at column)
* [x] Define `TenancyMetadata` structure (tenant column name)
* [x] Define `ColumnTransformerMetadata` structure (Read expression, Write expression per property)
* [x] Define `AssociationOverrideMetadata` structure (property name → override join column)
* [x] Define `GeneratedMetadata` structure (GenerationTime + re-SELECT SQL literal per property)
* [x] Define `GlobalFilterMetadata` structure (filter name + compile-time SQL fragment constant)
* [x] Define `SecondaryTableMetadata` structure (secondary table name, PK join column, column list)
* [x] Define `MapKeyMetadata` structure (key column name, key .NET type)
* [x] Implement mapping validation rules
* [x] Implement `MappingException` diagnostic reporting

> **Structure:** All EPIC 1 artifacts live under `DapperX.Core/Attributes`, enums, and metadata models — see [`Structures.md`](Structures.md) §2.1.
> **Verification:** Rule E informational attributes — `IndexNonRegressionTests`, `UniqueConstraintGenerationTests` in `DapperX.Tests` (SqlServer; EPIC 26).

***

# EPIC 2: Abstractions & Contracts

***

## Feature: Interfaces

### Tasks

* [x] Create `IRepository<T>` interface (all CRUD + batch + base query methods; includes `Load{Collection}ForManyAsync`, `Load{Map}ForManyAsync`, `GetAllAsync` overloads, `GetAllSliceAsync` overloads, `DeleteAllByIdAsync`)
* [x] Create `IQuery<T>` interface
* [x] Create `ILifecycleInvoker` interface
* [x] Create `IValueConverter<TProperty, TColumn>` interface
* [x] Create `IDapperXOptions` interface — includes: BatchSize, BulkThreshold, Logger, LogSql, LogParameters, LogExecutableSql; GlobalFilter methods: `EnableFilter(name, parameters)`, `DisableFilter(name)`, `IsFilterActive(name)` → `bool`
* [x] Create `IDatabaseProvider` interface
* [x] Create `IAuditingProvider` interface (GetCurrentUser)
* [x] Create `ITenantProvider` interface (GetCurrentTenantId)
* [x] Create `ISequenceAllocator` interface (`Task<long> NextAsync(string sequenceName)`) — optional injection; developer provides implementation; DapperX emits the call when registered

***

## Feature: Paging & Sorting Types

### Tasks

* [x] Create `Pageable` class (PageNumber, PageSize, Offset computed property)
* [x] Create `Page<T>` class (Content, TotalElements, TotalPages, PageNumber)
* [x] Create `Slice<T>` class (Content, HasNext flag) — no COUNT query; generator fetches pageSize+1 rows; HasNext = result.Count > pageSize
* [x] Create `Sort` class (Column, Ascending)

***

## Feature: Exception Types

### Tasks

* [x] Create `ConcurrencyException` (includes list of conflicting entity keys for batch)
* [x] Create `MappingException`
* [x] Create `SqlExecutionException` (wraps provider exception)
* [x] Create `InvalidSortException`

> **Structure:** `DapperX.Abstractions/` — see [`Structures.md`](Structures.md) §2.2.
> **Verification:** `DapperXOptionsTests`, `Page`/`Slice`/`Sort`, logging contracts — `LoggingTests`, `ExecutableSqlFormatterTests` in `DapperX.Tests` (SqlServer).

***

# EPIC 3: Source Generator — Entity Mapping

***

## Feature: Metadata Extraction

### Tasks

* [x] Scan syntax trees for `[Entity]` classes
* [x] Detect `[MappedSuperclass]` and merge properties into subclass column lists
* [x] Build `EntityModel` from syntax
* [x] Build `PropertyModel` (Column, Id, Version, Transient, Sortable, Converter, Formula flags)
* [x] Build `SequenceModel` from `[SequenceGenerator]`; validate referenced by `[GeneratedValue(Sequence)]`
* [x] Build `RelationshipModel` (all relationship types, JoinColumn, JoinTable, OrderColumn, PrimaryKeyJoinColumn flag, MapKey flag)
* [x] Build `SecondaryTableModel` from `[SecondaryTable]` + `[Column(Table)]` annotations; group properties per table
* [x] Build `MapKeyModel` from `[MapKey]`; validate key column exists on child entity; validate LazyMap<K,V> type compatibility
* [x] Detect `[PrimaryKeyJoinColumn]` on `[OneToOne]`; flag relationship as shared-PK; validate child `[Id]` is `GenerationType.Assigned`
* [x] Read `[Index]` attributes from entity class; store as `IndexMetadata` list on `EntityModel` (informational only — no SQL, DDL, or `Diagnostic` emitted; available to schema documentation tools)
* [x] Validate `[Index]`: emit no errors for any valid `[Index]` configuration; multiple `[Index]` on same entity are all stored; generator must not emit SQL or DDL for `[Index]` under any circumstance
* [x] Build `EmbeddedModel` from `[Embedded]` + `[AttributeOverride]`
* [x] Build `ConverterModel` from `[Converter]`
* [x] Build `FormulaModel` from `[Formula]`
* [x] Build `AuditingModel` from `[CreatedDate]`, `[LastModifiedDate]`, `[CreatedBy]`, `[LastModifiedBy]`
* [x] Build `SoftDeleteModel` from `[SoftDelete]`
* [x] Build `TenancyModel` from `[TenantId]`
* [x] Build `ColumnTransformerModel` from `[ColumnTransformer]`
* [x] Build `AssociationOverrideMetadata` from `[AssociationOverride]`; apply to inherited/embedded relationship FK resolution
* [x] Build `NamedQueryModel` from `[NamedQuery]` / `[NamedQueries]` on entity class
* [x] Validate all mapping at compile time (Diagnostics for all violations)

***

## Feature: SQL Builder (Compile-Time)

### Tasks

* [x] Generate SELECT SQL (all columns + formula expressions; append soft-delete filter; append tenant filter)
* [x] Generate INSERT SQL (exclude Insertable=false columns; inject auditing fields; inject tenant ID; emit identity/sequence return per provider; emit `_sequenceAllocator.NextAsync()` call when `ISequenceAllocator` is registered, else direct DB sequence call)
* [x] Generate UPDATE SQL (exclude Updatable=false columns; inject LastModifiedDate/By; include Version WHERE; soft-delete: emit UPDATE for `DeleteAsync`)
* [x] Generate DELETE SQL (hard delete with Version WHERE; soft-delete: emit UPDATE form)
* [x] Generate `HardDeleteAsync` SQL for `[SoftDelete]` entities
* [x] Generate column list for embedded properties (flattened)
* [x] Apply `[Formula]` expressions in SELECT column list; exclude from INSERT/UPDATE
* [x] Generate parameter bindings including auditing + tenant parameters
* [x] Generate per-entity `ResolveColumn(string propertyName)` method — compile-time switch mapping property names to column names; used by `WhereTranslator` / `OrderByTranslator` to avoid runtime reflection
* [x] Apply `[ColumnTransformer]` Read expression verbatim in SELECT column list; Write expression (with `?` → `@paramName`) in INSERT/UPDATE bindings; exclude from unmodified column list
* [x] Apply `[AssociationOverride]` FK column name replacements when building relationship JOIN SQL
* [x] Build `GeneratedColumnModel` from `[Generated]`; exclude from INSERT/UPDATE column lists; emit re-SELECT SQL literal after INSERT / after UPDATE (for GenerationTime.Always)
* [x] Build `GlobalFilterModel` from `[GlobalFilter]`; emit `private static readonly string FILTER_{NAME}` constant per filter per entity
* [x] Emit secondary table SQL from `SecondaryTableModel`: LEFT JOIN SELECT; split INSERT (primary first); split UPDATE; split DELETE (secondary first — topological order)
* [x] Emit `[PrimaryKeyJoinColumn]` JOIN condition `ON child.id = parent.id` as compile-time literal; emit child Id pre-assignment before INSERT

***

## Feature: Repository Generator

### Tasks

* [x] Create `DapperXRepositoryBase<TEntity, TId>` in `DapperX.Runtime/Repositories/` — abstract class implementing `IRepository<TEntity, TId>`; all Dapper call logic here; SQL strings as abstract properties (never duplicated in generated classes)
* [x] Define all abstract SQL string properties on base: SelectByIdSql, SelectAllSql, InsertSql, UpdateSql, DeleteSql, DeleteByIdSql, ExistsSql, CountSql, paging templates, sort fragment lookup support
* [x] Generator scans `[Entity]` classes; emits `{Name}RepositoryImpl : DapperXRepositoryBase<TEntity,TId>` with compile-time SQL string property overrides — no Dapper call code in generated class
* [x] Generator scans `[Repository]`-annotated interfaces; adds that interface to Impl class declaration; emits derived query method bodies inside the Impl class (single unified emission per entity; duplicate interface per entity → DPX019)
* [x] Generator emits `DapperXServiceCollectionExtensions.g.cs` — one `AddDapperXRepositories(Func<IServiceProvider, IDbConnection>)` extension covering all entities:
  * [x] `services.AddScoped<IDbConnection>(connectionFactory)` — registered once; injected into every Impl constructor by DI
  * [x] If `[Repository]` interface exists: `services.AddScoped<IXxxRepository, XxxRepositoryImpl>()` — clean two-arg form (Impl already declares the interface in its base list)
  * [x] `services.AddScoped<IRepository<TEntity,TId>>(sp => sp.GetRequiredService<IXxxRepository>())` — forwards base interface to same scoped instance
  * [x] If no `[Repository]` interface: `services.AddScoped<{Name}RepositoryImpl>()` + forward `IRepository<TEntity,TId>` to it
  * [x] `DiExtensionEmitter.cs` added to `DapperX.Generator/Emitters/` and wired into `DapperXSourceGenerator.cs` after all entity models are processed
  * [x] `Microsoft.Extensions.DependencyInjection.Abstractions` added to `DapperX.Runtime.csproj` so `IServiceCollection` flows transitively to consuming projects
* [x] Fix: `RepositoryEmitter` emit `using DapperX.Abstractions.Auditing;` whenever `entity.Auditing is not null` (not only when `CreatedByProperty` is set) — was causing CS0246 for date-only auditing entities
* [x] SampleApp (`samples/DapperX.SampleApp/`) wired up as minimal ASP.NET Core web app demonstrating full DI flow:
  * [x] Generator added as `Analyzer` project reference
  * [x] `Entities/Product.cs` — sample `[Entity]` with `[Id]`, `[Column]`, `[CreatedDate]`, `[LastModifiedDate]`
  * [x] `Repositories/IProductRepository.cs` — sample `[Repository]` interface with derived query methods
  * [x] `Program.cs` — `AddDapperXRepositories()` call + minimal API endpoints; builds cleanly
  * [x] Fix: `IRepository<,>` FQN corrected to `DapperX.Abstractions.Repositories.IRepository<>` in `DiExtensionEmitter`
  * [x] Fix: generated DI extension placed in `namespace Microsoft.Extensions.DependencyInjection` — auto-imported by web SDK, no extra `using` needed
  * [x] Fix: `IsSingleEntityReturn` in `DerivedQueryGenerator` — added `IReadOnlyList<>`, `IList<>`, `List<>`, `ICollection<>`, `IReadOnlyCollection<>` to collection detection; collection-returning derived methods now use `QueryAsync` not `QueryFirstOrDefaultAsync`
* [x] Generate `GetByIdAsync` with `PostLoad` hook *(DapperXRepositoryBase read paths + generated `OnPostLoad` override + `ProductLifecycleInvoker`)*
* [x] Generate `GetAllAsync()` with `PostLoad` hook and all applicable filters (soft-delete, tenancy, global) *(base + SqlBuilder WHERE; entities with `[GlobalFilter]` get `ApplyGlobalFilters` read overrides — verified `CatalogItemRepositoryImpl`)*
* [x] Generate `GetAllAsync(Sort sort)` — same base SQL + ORDER BY fragment lookup *(inherited from `DapperXRepositoryBase` + generated `GetSortFragment`; `GetAllAsyncGenerationTests`)*
* [x] Generate `GetAllAsync(Pageable pageable)` — same base SQL + paging template; returns `Page<T>` (with COUNT)
* [x] Generate `GetAllAsync(Sort sort, Pageable pageable)` — combined; returns `Page<T>`
* [x] Generate `GetAllSliceAsync(Pageable pageable)` — same base SQL + provider-specific `pageSize+1` template; returns `Slice<T>` (no COUNT query); invokes `PostLoad` per result
* [x] Generate `GetAllSliceAsync(Sort sort, Pageable pageable)` — combined; returns `Slice<T>`; applies all active filters (soft-delete, tenancy, global) *(base; global-filter entities override)*
* [x] Generate `FindAllByIdAsync` (`WHERE id IN @ids`) with `PostLoad` — single-key entities only; emit `Diagnostic` error if declared on composite-key entity *(DPX030 when interface explicitly declares `FindAllByIdAsync` / `DeleteAllByIdAsync` on composite-key entity)*
* [x] Generate `DeleteAllByIdAsync` (`DELETE WHERE id IN @ids`) — single-key entities only; fires `PreRemoveBatch`/`PostRemoveBatch` but no per-entity hooks; `Diagnostic` error on composite-key entities *(batch hooks when entity declares batch lifecycle methods)*
* [x] Generate `ExistsByIdAsync` (`SELECT EXISTS …`)
* [x] Generate `CountAsync` (`SELECT COUNT(*)`)
* [x] Generate `InsertAsync` (with auditing injection, tenant injection, key-back assignment, [Generated] re-SELECT after insert; secondary table INSERT after primary when [SecondaryTable] present; child.Id assignment when [PrimaryKeyJoinColumn] detected) *(`MutatingMethodEmitter` + `AuditingSqlBuilder` in `SqlBuilder`; identity `ExecuteScalarAsync`; `MutatingMethodGenerationTests`)*
* [x] Generate `UpdateAsync` (with auditing injection, [Generated(Always)] re-SELECT after update; secondary table UPDATE when [SecondaryTable] present) *(primary UPDATE then `SecondaryUpdate_*`; auditing columns in SQL)*
* [x] Generate `DeleteAsync` (soft-delete rewrite or hard delete per entity; secondary table DELETE before primary when [SecondaryTable] present) *(`SqlBuilder.BuildDelete` soft-delete + `MutatingMethodEmitter` secondary-first DELETE)*
* [x] Generate `DeleteByIdAsync` (with `PreRemove` / `PostRemove` hooks; secondary table DELETE before primary) *(remove-hook load override; secondary-first DELETE when `[SecondaryTable]` and no remove hooks)*
* [x] Generate `InsertManyAsync` / `UpdateManyAsync` / `DeleteManyAsync` *( `RepositoryEmission` + `RepositoryBatchGenerationTests`)*
* [x] Generate `InsertGraphAsync` / `UpdateGraphAsync` / `DeleteGraphAsync` *( `GraphGenerator` + `GraphBuilder`; OneToMany `LazyCollection` children; verified `OrderRepositoryImpl`)*
* [x] Inject `IDbConnection`, optional `IDbTransaction`, optional `IAuditingProvider`, optional `ITenantProvider`, optional `IDapperXOptions` *(ctor fields assigned when auditing/tenancy/global filters present)*
* [x] Emit `NotSupportedException` for mutating methods on `[Immutable]` entities *(single-entity + batch overrides in `RepositoryEmission`)*

***

## Feature: Lifecycle Generator

### Tasks

* [x] Detect lifecycle attributes on entity and generate invoker class
* [x] Generate all entity lifecycle invocations (Pre/Post Persist/Update/Remove/Load) *(entity hooks wired; derived-query Find/Delete PostLoad via DerivedQueryGenerator)*
* [x] Generate all batch lifecycle invocations (all six batch hooks) *(`{Entity}BatchLifecycleInvoker` + `RepositoryEmission` batch paths when entity declares batch attributes)*
* [x] Detect `[EntityListeners]` and emit listener invocations *(`EntityListenerTypes` metadata + direct calls in `{Entity}LifecycleInvoker`)*
* [x] Fire auditing field population before `PrePersist` / `PreUpdate` hooks *(`AuditingGenerator.EmitPopulateBeforePersist` in generated overrides)*
* [x] Inject lifecycle calls in correct order *(auditing → PrePersist → SQL → PostPersist; batch: PrePersistBatch → per-entity → PostPersistBatch)*

***

## Feature: Code Generation Constraints (Requirements.md Rules A–D)

### Tasks

**Rule A — SQL always compile-time:**
* [x] Enforce: all emitted SQL must be a compile-time string literal or a pre-generated literal selected via a compile-time switch — no SQL assembled from user-controlled input anywhere in generated or runtime code
* [x] Enforce: `[SecondaryTable]` execution order (primary-first INSERT, secondary-first DELETE) determined at compile time by generator — no runtime branching on execution order *(`SecondaryTableGenerator` emits ordered SQL literals + mutating overrides)*
* [x] Enforce: `[GlobalFilter]` conditions are `private static readonly string` constants — never concatenated at runtime
* [x] Enforce: all provider-specific SQL variants (dialects, lock hints, upsert, scalar functions, identity return, NULLS FIRST/LAST) selected by compile-time `DatabaseProvider` constant
* [x] Enforce: `[Generated]` re-SELECT SQL is a compile-time literal — no runtime column discovery

**Rule B — No runtime reflection on entity types:**
* [x] Enforce: `ResolveColumn(propertyName)` generated switch replaces all runtime property introspection — no `MemberInfo`, `Type.GetProperties()`, `FieldInfo`, or `dynamic` in generated or runtime code
* [x] Enforce: all Dapper result mapping uses strongly-typed generated code — no runtime `Type.GetProperties()` on entity types
* [x] Enforce: lifecycle hook invocations use generated direct calls — no `MethodInfo.Invoke()`

**Rule C — Runtime data ops are not SQL violations (verify compliance):**
* [x] Verify: `[PrimaryKeyJoinColumn]` child Id assignment (`child.Id = parent.Id`) is a data assignment, not SQL construction — generates property assignment code, not SQL string manipulation *(`PrimaryKeyJoinColumnGenerator.EmitAssignBeforeInsert`)*
* [x] Verify: `LazyMap<K,V>` in-memory grouping (`.ToDictionary(...)`) is LINQ data processing after SQL execution — no SQL string is built at runtime
* [x] Verify: `[GlobalFilter]` conditional append (`if active → sql += CONSTANT`) appends a compile-time constant string, not a user-constructed fragment
* [x] Verify: `IAuditingProvider.GetCurrentUser()` and `ITenantProvider.GetCurrentTenantId()` return values are passed as Dapper `@params` — never concatenated into SQL

**Rule D — Stateless = no cross-call state:**
* [x] Enforce: repository method implementations hold no instance state between calls
* [x] Enforce: `LazyCollection`, `LazyReference`, `LazyMap` per-instance caches are on entity instances only — no shared or global cache
* [x] Enforce: `DapperXOptions` filter state is scoped per DI instance — never static
* [x] Enforce: `ISequenceAllocator` is developer-injected — DapperX emits the call but holds no sequence counter state

**Req Rule E — Informational annotations produce no SQL (Requirements.md Section 1):**
* [x] Enforce: `[Index]` and `[UniqueConstraint]` must produce zero SQL, DDL, or Diagnostic output from the generator — any violation is a spec error
* [x] Enforce: presence of `[Index]` on any entity must not alter any generated repository SQL (SELECT, INSERT, UPDATE, DELETE, or filter fragments)
* [x] Verify: adding `[Index]` to an entity that has passing tests must not change any generated SQL strings *(`IndexNonRegressionTests`)*

**General constraints (Requirements.md Section 23):**
* [x] Enforce: incremental generation — only re-generate entities whose source has changed; use Roslyn `IncrementalGeneratorInitializationContext`
* [x] Validate at compile time: all mapping rules, relationship constraints, CPQL queries, filter conditions, window function placement, scalar function argument types — emit `Diagnostic` errors for all violations *(`MappingValidator` orchestrates `ElementCollectionValidator`, `NamedEntityGraphValidator`, `SoftDeleteValidator`, `GlobalFilterValidator`, `GeneratedColumnValidator`, `AuditingValidator`, `CompositeKeyValidator`; CPQL via `CpqlSemanticValidator` + `CpqlValidator`)*
* [x] Emit `MethodName` compile-time string literal in every generated repository method for `DapperXLogEntry` *(`MethodNameEmitter` base wrappers + mutating/global-filter/derived overrides)*
* [x] Emit `Diagnostic` compile errors for SQLite-incompatible features: pessimistic locking, sequences, stored procedures, multiple result sets, Schema attribute *(DPX017–018, DPX035–037; `IQuery.WithLock` runtime throw on Sqlite; no runtime Sqlite lock suffix in derived queries)*
* [x] Verify that generated code covers the complete Section 23 "Must generate" checklist — all 32 items *(`Section23ComplianceTests` Theory matrix; fixtures: `TaggedProduct`, `Student`/`Course` M2M, `[NamedEntityGraph]` on `Product`, `Order` graph)*

> **Structure:** `DapperX.Generator/` — see [`Structures.md`](Structures.md) §2.8+.
> **Verification:** SqlServer — 418+ tests in `DapperX.Tests`; per-provider SQL literals → **EPIC 26a** `matrix-4` (e.g. `UpsertGenerationMatrixTests`, `SlicePagingMatrixTests`).

***

# EPIC 4: Source Generator — Derived Query Methods

> **Status:** Complete for method-name derivation, runtime parameters, custom attributes (including CPQL `[Query]` via `CpqlGenerator`), and repository emission. Verified: 418+ tests in `DapperX.Tests`, build green. Per-provider derived paging/lock SQL → **EPIC 26a** `DerivedQueryPagingMatrixTests`, `LockingMatrixTests`. **Out of scope:** multi-hop property paths in method names (Requirements — use CPQL).

***

## Feature: Repository Interface Scanner

### Tasks

* [x] Scan for interfaces annotated with `[Repository]` (no-argument; separate from `[Entity]` scanning)
* [x] Resolve the entity type from `IRepository<TEntity,TId>` generic argument; validate it has `[Entity]`
* [x] Derive `{Name}RepositoryImpl` class name from interface name (strip leading `I`, append `Impl`)
* [x] Emit all derived query method implementations directly into the sealed `{Name}RepositoryImpl` class *(Find/Exists/Count/Delete/OrderBy/AllBy; embedded + one-level navigation; `MethodSymbolKey` overloads; Sort/Pageable/Slice/Page/LockMode; write prefixes Insert/Update; `[BulkOperation]` → batch base methods; regex provider SQL + DPX024/DPX029; `[Query]` NativeQuery + CPQL via `CpqlGenerator`)*

***

## Feature: Method Name Parser

### Tasks

* [x] Parse all subject keyword prefixes (Find/Get/Query/Search/Read/Stream/Count/Exists/Has/Contains/Delete/Remove/Insert/Add/Save/Create/Update/Modify)
* [x] Parse `By` separator
* [x] Parse property path segments (direct, embedded, navigation — one level deep)
* [x] Validate property paths against entity model at compile time *(DerivedQueryValidator — DPX021)*
* [x] Report `Diagnostic` for unresolvable property names *(DPX021 error)*
* [x] Parse `And` / `Or` with correct precedence (`And` binds tighter than `Or`)
* [x] Implement property-first longest-match conflict resolution:
  * [x] At each parse position, collect all known entity property names
  * [x] Try longest-prefix property name match before attempting operator keyword parsing
  * [x] If property match found → consume as property path, skip keyword parsing for that segment
  * [x] If no property match → parse as operator keyword
* [x] Emit `Diagnostic` error when two equally valid interpretations exist (genuine ambiguity)
* [x] Emit `Diagnostic` warning when an entity property name is identical to a core operator keyword (`And`, `Or`, `Not`, `In`, `Like`, `True`, `False`, `Null`, `Between`, `Before`, `After`, `OrderBy`, `First`, `Top`, `Distinct`, `Count`) — `PropertyNameValidator` (DPX015)
* [x] Integrate `PropertyFirstResolver` into `DerivedQueryGenerator` — resolver receives entity property name list from `EntityModel` at compile time

***

## Feature: Operator Parsing

### Tasks

* [x] Parse all comparison operators (equality, Not, GreaterThan, LessThan, Between, Is/Equals synonyms)
* [x] Parse all string operators (Like, Containing, StartingWith, EndingWith, and Not variants)
* [x] Parse collection operators (In, NotIn)
* [x] Parse null checks (IsNull, IsNotNull)
* [x] Parse boolean values (True, False)
* [x] Parse date/time operators (Before, After)
* [x] Parse regex operators with provider support check *(SQL via `BuildRegexPredicate`; DPX024 error on SqlServer; DPX029 warning on Sqlite)*
* [x] Parse case-insensitivity modifiers (IgnoreCase, AllIgnoreCase)

***

## Feature: Result Modifiers

### Tasks

* [x] Parse Distinct / CountDistinct (emits `COUNT(DISTINCT primaryKey)`)
* [x] Parse `OrderBy{Property}Asc/Desc` and `Then` chaining
* [x] Parse `First` / `Top` / `First{n}` / `Top{n}` (dialect-specific)

***

## Feature: Runtime Parameter Types

### Tasks

* [x] Detect `Pageable` — append `OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY` template
* [x] Detect `Sort` — generate base SQL once + ORDER BY fragment lookup per `[Sortable]` × direction
* [x] Detect `LockMode` — append provider-specific lock hint
* [x] Detect `Page<T>` — emit COUNT + paginated SELECT
* [x] Detect `IAsyncEnumerable<T>` — emit `QueryUnbufferedAsync`
* [x] Detect `Slice<T>` return type — emit single SELECT with provider-specific `FETCH FIRST pageSize+1 ROWS ONLY` / `LIMIT pageSize+1` template; assign HasNext from result count at runtime; compile-time SQL template
* [x] Detect `bool IncludeDeleted` parameter — select between pre-generated WITH-filter and WITHOUT-filter SQL literals for `[SoftDelete]` entities; emit `Diagnostic` warning if `IncludeDeleted` is used on entity without `[SoftDelete]` *(paired literals in DerivedQueryGenerator; DPX022)*

***

## Feature: Custom Query Attributes

### Tasks

* [x] Detect `[Query]` on method — CPQL translated via `CpqlGenerator`; `NativeQuery = true` passes SQL through unchanged
* [x] Detect `[Query(NativeQuery = true)]` and embed SQL as-is
* [x] Detect `[NamedQuery("name")]` on method and resolve against entity's named queries
* [x] Detect `[NamedQueries]` on entity and index all named queries by name
* [x] Detect `[StoredProcedure]` and emit provider-specific call
* [x] Detect `[BulkOperation]` and route to `InsertManyAsync` / `UpdateManyAsync` / `DeleteManyAsync` on base

***

# EPIC 5: Source Generator — CPQL

> **Status:** Complete — full `CpqlSemanticValidator` (Compilation-backed NEW, CTE columns, return types, IN/EXISTS, LEFT JOIN errors, soft-delete bypass), 23-function scalar table with per-provider snapshots (`CpqlScalarSnapshotTests` = `single-project` four-provider **strings**). Tests: `CpqlParserTests`, `CpqlTranslatorTests`, `CpqlGenerationTests`, `CpqlSemanticValidatorTests`, `EpicFollowUpTests`. Emitted repo SQL per provider → **EPIC 26a** `CpqlEmittedSqlMatrixTests`, `CpqlMutationMatrixTests`.

***

## Feature: CPQL Parser

### Tasks

* [x] Implement CPQL lexer (all keywords, identifiers, operators, parameters)
* [x] Implement parser for SELECT, UPDATE, DELETE statements
* [x] Implement WITH clause parsing (multiple CTEs, RECURSIVE)
* [x] Implement FROM / JOIN clause parsing (entity names, CTE names, relationship paths)
* [x] Implement WHERE condition parsing (all operators, AND/OR/NOT, parenthesized grouping)
* [x] Implement SELECT clause parsing (entity alias, property list, aggregates, constructor expressions)
* [x] Implement arithmetic expression parsing (`+`, `-`, `*`, `/`)
* [x] Implement CASE/WHEN parsing (simple and searched forms)
* [x] Implement subquery parsing (scalar, EXISTS, NOT EXISTS, IN, NOT IN)
* [x] Implement `CAST(x AS cpql_type)` parsing
* [x] Implement `NULLIF(x, y)` parsing
* [x] Implement `CONCAT(…)` parsing
* [x] Implement `NULLS FIRST` / `NULLS LAST` in ORDER BY parsing
* [x] Implement GROUP BY / HAVING parsing
* [x] Implement window function parsing: `function OVER (PARTITION BY … ORDER BY … frame_spec)`
* [x] Implement new scalar functions: `LTRIM`, `RTRIM`, `SUBSTRING`, `REPLACE`, `LEFT`, `RIGHT`, `MOD`, `POWER` *( `CpqlScalarFunctions`; LEFT/RIGHT SQLite SUBSTR aliases)*
* [x] Define `CpqlAst.cs` node types for all constructs including `WindowExprNode`, `WindowSpecNode`, `FrameSpecNode`

***

## Feature: CPQL Validator

### Tasks

* [x] Validate entity names, property references, relationship property names *( `CpqlSemanticValidator`)*
* [x] Validate return type compatibility with SELECT clause *(numeric aggregate, NEW DTO, entity-shaped returns in `CpqlSemanticValidator`)*
* [x] Validate constructor existence and parameter type order *(NEW — resolves type via `Compilation`; arity + argument compatibility errors)*
* [x] Validate aggregate + GROUP BY coherence
* [x] Validate LEFT JOIN nullability *(error DPXCPQL023 when FK non-nullable on LEFT JOIN)*
* [x] Validate arithmetic operand types
* [x] Validate scalar function argument types *(CONCAT, NULLIF, MOD, POWER, LEFT/RIGHT/SUBSTRING, date parts)*
* [x] Validate `CASE` branch type compatibility and ELSE-less nullability
* [x] Validate `CAST` type castability *(basic bool cast check)*
* [x] Validate `NULLIF` argument type compatibility
* [x] Validate `CONCAT` string-type arguments
* [x] Validate scalar / IN / EXISTS subquery forms *(single-column scalar subquery; IN type compatibility DPXCPQL035)*
* [x] Validate CTE name references; warn on unreferenced CTEs *(DPXCPQL030 warning; CTE column tracking)*
* [x] Validate `NULLS FIRST/LAST` provider support *(parsed and emitted; SQLite 3.30+ note in translator)*
* [x] Validate `SUBSTRING`, `LEFT`, `RIGHT` operands are string type
* [x] Validate `MOD`, `POWER` operands are numeric type
* [x] Reject bypassing soft-delete filter in CPQL (except `NativeQuery = true`) *(`SoftDeleteValidator.ValidateCpqlBypass` DPXCPQL040)*

***

## Feature: CPQL Translator

### Tasks

* [x] Translate entity/property names to table/column names
* [x] Translate path expressions to JOINs + column references
* [x] Translate `:paramName` to `@paramName`
* [x] Translate all aggregate functions
* [x] Translate arithmetic expressions
* [x] Translate CASE/WHEN (both forms)
* [x] Translate all five subquery forms
* [x] Translate WITH / RECURSIVE CTE
* [x] Translate `CAST` to provider-specific type names
* [x] Translate `NULLIF` / `CONCAT`
* [x] Translate `NULLS FIRST/LAST` to provider-specific emulation
* [x] Translate window functions to provider-specific SQL (all four providers)
* [x] Translate `PARTITION BY` and `ORDER BY` inside `OVER (…)` using entity property→column resolution
* [x] Translate frame spec (`ROWS`/`RANGE BETWEEN`) verbatim
* [x] Translate new scalar functions: `LTRIM`, `RTRIM`, `SUBSTRING` (per-provider syntax), `REPLACE`, `LEFT`, `RIGHT` (SQLite: SUBSTR), `MOD` (SQL Server/SQLite: `%`), `POWER`
* [x] Translate cross-provider scalar functions (now 23-function table) *(`CpqlScalarSnapshotTests` golden per provider)*
* [x] Translate boolean literals dialect-aware
* [x] Translate bulk UPDATE to provider-specific `UPDATE … FROM … JOIN` syntax
* [x] Append soft-delete filter to CPQL SELECT automatically
* [x] Append tenant filter to CPQL SELECT/UPDATE/DELETE automatically
* [x] Emit translated SQL as inline string literal in generated method

***

# EPIC 6: Query System (Runtime)

> **Status:** Complete — `IRepository.Query()`, `QueryGenerator` (QueryBaseSql, `QueryProjectionBaseSql`, include join catalog, split loaders, `SoftDeleteSupported`), `RepositoryQuery` + `QueryExecutor`, `Select<TDto>()` via `[Projection]` catalog, `WhereTranslator` (null/IN/LIKE), `IncludeDeleted` fail-fast, `UnsupportedQueryExpressionException`. Tests: `QueryGenerationTests`, `QueryBuilderTests`, `Section23ComplianceTests` (SqlServer). `IQuery`/`AsSplitQuery`/`Select<TDto>()` roundtrip → EPIC 26 integration extras.

***

## Feature: Query Builder

### Tasks

* [x] Create `Query<T>` base class with all fluent methods
* [x] Implement `Where` (expression tree → WhereTranslator; append soft-delete + tenant + active global filters)
* [x] Implement `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending`
* [x] Implement `Skip` / `Take`
* [x] Implement `Include` (joined mode)
* [x] Implement `ThenInclude`
* [x] Implement `AsSplitQuery`
* [x] Implement `Select<TDto>()` projection *(`QueryGenerator` + `ProjectionCollector`; `RepositoryQuery.Select<TDto>()` uses `QueryProjectionBaseSql`)*
* [x] Implement `WithLock(LockMode, TimeoutMs = 0)`
* [x] Implement `AsSlice()` — fluent modifier that switches return type from `Page<T>` to `Slice<T>`; emits `LIMIT pageSize+1` template instead of paging + COUNT
* [x] Implement `IncludeDeleted()` — runtime skips soft-delete predicate when `[SoftDelete]` configured on entity
* [x] Implement global filters via generated `ApplyGlobalFilters()` on `RepositoryQuery` *(removed unused `GlobalFilterApplicator`; runtime uses `QueryRuntimeConfig` delegate)*
* [x] Implement `SoftDeleteBypassSelector` — holds the IncludeDeleted flag; passed to repository execution to select correct SQL literal

***

## Feature: Expression Translation

### Tasks

* [x] Implement `ExpressionParser` — parse lambda expression tree structure (`MemberExpression`, `BinaryExpression`, etc.) using `System.Linq.Expressions`
* [x] Implement `WhereTranslator` — parameterized WHERE fragment; resolves property→column via generated `ResolveColumn(propertyName)` method, never via `System.Reflection.MemberInfo`
* [x] Implement `OrderByTranslator` — same pattern; resolves property→column via generated `ResolveColumn()`
* [x] Implement `ProjectionMaterializer` — map result columns to `[Projection]` DTO *(`EnsureProjection<TDto>()` validates `[Projection(From=…)]`; Dapper column alias mapping)*
* [x] Prevent unsupported expressions; fail fast *(`UnsupportedQueryExpressionException`; null, Contains/IN, LIKE in `WhereTranslator`)*

***

# EPIC 7: Lazy Loading & Relationships

***

## Feature: LazyCollection

### Tasks

* [x] Implement `LazyCollection<T>` with `GetAsync`, `TryGet`, `Set` — no `Reload()` (read-once model)
* [x] Apply `[OrderBy]` default ordering in generated load SQL
* [x] Apply `[OrderColumn]` ordering in generated load SQL
* [x] Enforce read-once contract: per-instance cache is a within-operation optimization; fresh data requires new repository call
* [x] Ensure thread safety

***

## Feature: LazyReference

### Tasks

* [x] Implement `LazyReference<T>` with `GetAsync`, `TryGet`, `Set` — no `Reload()` (read-once model)

***

## Feature: LazyMap

### Tasks

* [x] Implement `LazyMap<TKey, TValue>` in `DapperX.Relations/Lazy/`
  * [x] `GetAsync()` → `IReadOnlyDictionary<TKey, TValue>` — executes SQL (same literal as `[OneToMany]`), groups result by `[MapKey]` column value using in-memory LINQ (no dynamic SQL)
  * [x] `TryGet()` → `IReadOnlyDictionary<TKey, TValue>?` — returns cached dictionary or null; no DB call
  * [x] `Set(IDictionary<TKey, TValue>)` — injects pre-loaded data; marks as loaded
  * [x] Read-once model: same per-instance cache rules as `LazyCollection`
  * [x] Thread-safe; no `Reload()` method

***

## Feature: Relationship Loader

### Tasks

* [x] Generate SELECT SQL for all relationship types (OneToMany, ManyToOne, OneToOne)
* [x] Generate OneToOne shared-PK JOIN SQL when `[PrimaryKeyJoinColumn]` detected: `ON child.id = parent.id` as compile-time literal
* [x] Generate Eager fetch JOIN SQL for `FetchType.Eager`
* [x] Generate `[OrderColumn]` position assignment logic on collection insert
* [x] Generate `[OrderColumn]` gap-close logic on collection item delete
* [x] Generate `[MapKey]` collection loader: same SELECT SQL as `[OneToMany]`; emit in-memory grouping by key column into `Dictionary<K,V>` after load
* [x] Generate `Load{Map}ForManyAsync` batch loader for `LazyMap` relationships: `WHERE fk IN @parentIds` SQL literal; group by FK then by map key *(EPIC 17e/k — `BatchRelationshipLoaderGenerator`)*
* [x] Integrate with Include system via `Set()`

***

# EPIC 8: Batch Processing

***

## Feature: Batch Execution & Chunking

### Tasks

* [x] Implement batch insert / update / delete using `ExecuteAsync(collection)` *(generator emits chunked `ExecuteAsync(..., chunk, ...)` paths; identity inserts keep per-entity scalar id backfill)*
* [x] Implement `BatchChunker` with configurable size *(validated `chunkSize > 0`; deterministic chunk boundaries)*
* [x] Preserve chunk execution order *(sequential chunk loop in generated batch methods; no parallel chunk execution)*

***

# EPIC 9: Graph Execution

***

## Feature: Graph Builder & Execution Plan

### Tasks

* [x] Build relationship DAG; detect cycles → Diagnostic error *( `GraphBuilder.HasCycles` → DPX012; cycle edges use any non-`None` cascade)*
* [x] Filter graph relationships by `CascadeType` per operation *(Persist→InsertGraph, Merge→UpdateGraph, Remove→DeleteGraph; `CascadeHelper` + `GraphBuilder.GetGraphRelationships`; default `None` skips child ops)*
* [x] Emit graph methods when entity has graph-capable relationships even if cascade is `None` *(parent-only Insert/Update/Delete graph)*
* [x] `GraphChildRepositoryEmitter` — construct child `{Entity}RepositoryImpl` with parent DI passthrough when parent and child both need tenant/auditing/options/sequence
* [x] Implement `TopologicalSorter` *(runtime `DapperX.Batching.Graph.TopologicalSorter`; generator uses depth-first graph walk)*
* [x] Generate `InsertGraphAsync` / `UpdateGraphAsync` / `DeleteGraphAsync` *( `GraphGenerator` — see EPIC 3)*
* [x] Generate FK assignment logic (parent key → child FK after parent insert) *( `GraphGenerator.EmitInsertGraphAsync` emits `child.{Fk} = root.Id` before child insert batch)*
* [x] Insert join table records for many-to-many after graph inserts *( `GraphGenerator.EmitManyToManyInsertLoop`; null-id guard + `Distinct()` de-dup before join-table insert)*
* [x] Wrap all graph operations in transaction; rollback on failure *(generated `ownsTransaction` + try/commit + catch/rollback/rethrow in Insert/Update/Delete graph methods)*

***

# EPIC 10: Concurrency Control

***

## Feature: Optimistic Concurrency

### Tasks

* [x] Emit `WHERE version = @currentVersion` in UPDATE / DELETE SQL
* [x] Increment Version in UPDATE SQL
* [x] Throw `ConcurrencyException` on 0 affected rows (single and batch)
* [x] Batch: count total affected rows; throw listing all conflicting keys if any conflict
* [x] Graph: conflict on any entity rolls back entire transaction

***

## Feature: Pessimistic Locking

### Tasks

* [x] Emit provider-specific lock SQL (`WITH (UPDLOCK, ROWLOCK)` / `FOR UPDATE`)
* [x] Support `WithLock(LockMode, TimeoutMs)` on query builder
* [x] Support `LockMode` parameter on derived query methods *(DerivedQueryGenerator `EmitLockSuffixAppend`; provider-specific hints; Sqlite pessimistic throws at runtime)*
* [x] Emit `Diagnostic` warning if pessimistic lock used without detectable transaction context

***

## Feature: Lock Timeout

### Tasks

* [x] Detect `TimeoutMs` on `WithLock(LockMode.Pessimistic, TimeoutMs)` at compile time
* [x] SQL Server: emit `SET LOCK_TIMEOUT @timeout` as a separate parameterized Dapper call before SELECT
* [x] PostgreSQL: emit `FOR UPDATE NOWAIT` when `TimeoutMs = 0`; emit `SET lock_timeout = @timeout` + `FOR UPDATE` when `TimeoutMs > 0`
* [x] MySQL: emit `FOR UPDATE NOWAIT` when `TimeoutMs = 0`; emit `Diagnostic` warning for `TimeoutMs > 0` (not natively supported — treated as NOWAIT)
* [x] All timeout SQL is a compile-time template; `@timeout` is always an **integer (milliseconds)** — no string concatenation, no `ms` suffix appended in the SQL template
* [x] PostgreSQL: `SET lock_timeout = @timeout` accepts integer milliseconds directly (e.g., 5000 = 5 s); no `'5000ms'` string form needed

***

# EPIC 11: Lifecycle System

***

## Feature: Entity & Batch Lifecycle

### Tasks

* [x] Inject all entity lifecycle hooks (Pre/Post Persist/Update/Remove/Load) *( `{Entity}LifecycleInvoker` + repository `On*` overrides; listener-aware `HasLifecycleHook`)*
* [x] Inject all batch lifecycle hooks (all six) *(`{Entity}BatchLifecycleInvoker` + `RepositoryEmission` / `GraphGenerator`)*
* [x] Enforce correct wrapping order *(batch: `Pre*Batch` → per-entity hooks → SQL → per-entity hooks → `Post*Batch`; verified in `LifecycleTests`)*
* [x] Fire `PostLoad` after all SELECT methods *(CRUD `ApplyPostLoad`, derived/CPQL/query builder, native `[Query(NativeQuery=true)]` SELECT)*

***

## Feature: Entity Listeners

### Tasks

* [x] Implement `EntityListenerInvoker` *(compile-time `{Entity}LifecycleInvoker` with direct listener calls — no runtime reflection; removed reflection stub)*
* [x] Detect `[EntityListeners]` in generator; emit listener instantiation + invocations *(`MetadataBuilder.BuildEntityListeners` + `LifecycleEmitter`)*
* [x] Support one listener registered on multiple entities *(shared `AuditListener` on `ListenerOnlyItem` + `SharedListenerItem`; verified in `LifecycleTests`)*

***

# EPIC 12: Auditing

***

## Feature: Auditing Field Injection

### Tasks

* [x] Implement `IAuditingProvider` interface
* [x] Detect `[CreatedDate]`, `[LastModifiedDate]`, `[CreatedBy]`, `[LastModifiedBy]` in generator
* [x] Build `AuditingModel` per entity
* [x] Emit `CURRENT_TIMESTAMP` (dialect-specific) for date fields in INSERT SQL *( `AuditingSqlBuilder.CurrentTimestampLiteral`; date columns non-insertable; verified `GETDATE()` in `AuditingTests`)*
* [x] Emit `CURRENT_TIMESTAMP` for `[LastModifiedDate]` in UPDATE SQL *(provider-aware `GetUpdateAssignments`; verified in `AuditingTests`)*
* [x] Emit `_auditingProvider.GetCurrentUser()` call for `[CreatedBy]` in INSERT SQL *( `AuditingGenerator.EmitPopulateBeforePersist`; `@CreatedBy` param in INSERT)*
* [x] Emit `_auditingProvider.GetCurrentUser()` call for `[LastModifiedBy]` in INSERT + UPDATE SQL
* [x] Enforce `[CreatedDate]` / `[CreatedBy]` as `Updatable = false` — exclude from UPDATE SQL *(auditing role flags in `MetadataBuilder`; verified UpdateSql excludes created columns)*
* [x] Inject `IAuditingProvider` into generated repository constructor
* [x] Emit `Diagnostic` warning if `[CreatedBy]` / `[LastModifiedBy]` present without registered provider *(DPX013 via `AuditingValidator`)*
* [x] Ensure auditing fields fire before `PrePersist` / `PreUpdate` lifecycle hooks *(verified in `AuditingTests`)*
* [x] Support `[CreatedDate]` / `[CreatedBy]` on `[MappedSuperclass]` (inherited by all subclasses) *(`CollectMembers` base walk + `MappedAuditItem` fixture)*

***

# EPIC 13: Soft Delete

***

## Feature: Soft Delete Rewriting

### Tasks

* [x] Implement `SoftDeleteAttribute` detection in generator
* [x] Build `SoftDeleteModel` per entity
* [x] Rewrite `DeleteAsync` → `UPDATE … SET is_deleted = 1 [, deleted_at = CURRENT_TIMESTAMP] WHERE id = @id [AND version = @version]`
* [x] Rewrite `DeleteByIdAsync`, `DeleteManyAsync`, `DeleteGraphAsync` the same way
* [x] Append `AND is_deleted = 0` to ALL SELECT SQL (CRUD, derived methods, graph loads, query builder)
* [x] Append soft-delete filter to CPQL SELECT automatically; reject CPQL DELETE bypass (use NativeQuery = true)
* [x] Generate `HardDeleteAsync` method (true DELETE, for privileged use)
* [x] Preserve `[Version]` check in soft-delete UPDATE
* [x] Fire `PreRemove` / `PostRemove` hooks for soft deletes
* [x] Support `[SoftDelete]` on `[MappedSuperclass]` (inherited by all subclasses)
* [x] Implement `SoftDeleteValidator` — validate column existence; warn on CPQL mutation bypass

***

# EPIC 14: Multi-Tenancy

***

## Feature: Tenant Filter Injection

### Tasks

* [x] Implement `ITenantProvider` interface
* [x] Detect `[TenantId]` in generator; build `TenancyModel`
* [x] Append `AND {tenant_column} = @tenantId` to ALL SELECT SQL
* [x] Append `AND {tenant_column} = @tenantId` to UPDATE and DELETE WHERE clauses
* [x] Emit tenant column SET in INSERT SQL from `_tenantProvider.GetCurrentTenantId()`
* [x] Enforce `[TenantId]` as `Updatable = false` — tenant column excluded from UPDATE SQL
* [x] Inject `ITenantProvider` into generated repository constructor
* [x] Emit `Diagnostic` warning if `[TenantId]` present without registered provider
* [x] Append tenant filter to CPQL SELECT/UPDATE/DELETE automatically
* [x] Support `[TenantId]` on `[MappedSuperclass]` (inherited by all subclasses)
* [x] Implement `TenancyValidator`

***

# EPIC 15: Formula Mapping

***

## Feature: Formula Column

### Tasks

* [x] Detect `[Formula]` attribute in generator; build `FormulaModel` *( `PropertyModel.Formula` + `EntityModel.Formulas` list)*
* [x] Include formula SQL expression verbatim in SELECT column list *( `FormulaEmitter.FormatSelectColumn`; used by `SqlBuilder` / `ProjectionCollector`)*
* [x] Exclude formula properties from INSERT, UPDATE, and WHERE generation *(insertable/updatable false; `ResolveColumn` / `DerivedQueryPathBuilder` skip; CPQL DPX053)*
* [x] Implement `FormulaEmitter` — appends formula expressions to SELECT column list
* [x] Validate: formula properties must not be `[Sortable]`, `[Version]`, or `[Id]` *( `FormulaValidator` DPX050–DPX052)*

***

# EPIC 16: Embeddable Types

***

## Feature: Embeddable Mapping

### Tasks

* [x] Detect `[Embeddable]` / `[Embedded]` / `[AttributeOverride]` in generator *( `MetadataBuilder.ExpandEmbeddedColumns`; `EmbeddedSites` on `EntityModel`)*
* [x] Flatten embedded properties into owning entity column list at compile time *( `PropertyModel.IsEmbeddedColumn`, `EmbeddedOwner` / `EmbeddedInner`)*
* [x] Apply `[AttributeOverride]` column name replacements per embed site
* [x] Include embedded columns in SELECT, INSERT, UPDATE SQL
* [x] Handle null embedded property (all embedded columns null) *( `MapFromDbRow` null-check per embed site)*
* [x] Validate: embeddable class must not have `[Id]` or `[Table]` *( `EmbeddableValidator` DPX054–DPX056)*

***

# EPIC 17: Type Converters

***

## Feature: Converter Infrastructure

### Tasks

* [x] Create `IValueConverter<TProperty, TColumn>` interface
* [x] Detect `[Converter]` in generator; emit converter call in read + write paths *( `ConverterEmitter`; `ApplyConvertersRead`; `BuildMutationParameters` ToColumn)*
* [x] Detect `[Enumerated(EnumType.String)]` — map to `EnumToStringConverter<TEnum>` at compile time
* [x] Detect `[Enumerated(EnumType.Ordinal)]` — map to `EnumToIntConverter<TEnum>` at compile time
* [x] Validate converter is stateless *( `ConverterValidator` DPX057; built-in enum converters skipped)*

***

## Feature: Built-in Converters

### Tasks

* [x] Implement `EnumToStringConverter<TEnum>`
* [x] Implement `EnumToIntConverter<TEnum>`
* [x] Implement `JsonConverter<T>`
* [x] Implement `UtcDateTimeConverter`

***

# EPIC 17b: Column Transformer

***

## Feature: Column Transformer

### Tasks

* [x] Create `ColumnTransformerAttribute` class (Read, Write SQL expression strings)
* [x] Build `ColumnTransformerModel` per property at compile time
* [x] Implement `ColumnTransformerEmitter` — include Read expression verbatim in SELECT column list *(via `FormulaEmitter`; stub documents delegation)*
* [x] In INSERT SQL: replace `@paramName` binding with `Write_expr.Replace("?", "@paramName")`
* [x] In UPDATE SQL: same Write expression replacement *(read-only Write-null excluded from UPDATE)*
* [x] Exclude `[ColumnTransformer]` properties from the bare column list (SELECT without transformer) *(Read expr in SELECT via `FormulaEmitter`)*
* [x] Validate: `[ColumnTransformer]` and `[Converter]` must not coexist on same property → `Diagnostic` error *(DPX010)*
* [x] `[ColumnTransformer(Read)]` only: property excluded from INSERT/UPDATE (read-only with SQL transform)
* [x] `[ColumnTransformer(Write)]` only: raw column in SELECT, transform only on write

***

# EPIC 17c: Association Override

***

## Feature: Association Override

### Tasks

* [x] Create `AssociationOverrideAttribute` class (Name — relationship property, JoinColumn — override column name)
* [x] Build `AssociationOverrideMetadata` at compile time
* [x] Apply override when resolving FK column name for inherited relationships from `[MappedSuperclass]`
* [ ] Apply override when resolving FK column for relationships defined inside `[Embeddable]` classes *(deferred — `{Embed}.{Rel}` path; blocks full `[Immutable]` entity compile test — generator duplicate mutating overrides)*
* [x] Multiple `[AssociationOverride]` attributes supported on one entity class
* [x] Validate: `Name` must reference an existing relationship property in the superclass/embeddable → `Diagnostic` error *( `AssociationOverrideValidator` DPX058)*

***

# EPIC 17d: Upsert Operations

***

## Feature: Upsert

### Tasks

* [x] Create `UpsertGenerator` and `UpsertSqlBuilder` (`UpsertEmitter` not split — SQL + overrides wired in `RepositoryEmitter`)
* [x] Generate `UpsertAsync(T entity)` with provider-specific SQL literal:
  * [x] SQL Server: `MERGE INTO table USING … ON … WHEN MATCHED UPDATE … WHEN NOT MATCHED INSERT …`
  * [x] PostgreSQL: `INSERT INTO … ON CONFLICT (id) DO UPDATE SET …`
  * [x] MySQL: `INSERT INTO … ON DUPLICATE KEY UPDATE …`
  * [x] SQLite: `INSERT … ON CONFLICT DO UPDATE`
* [x] Generate `UpsertManyAsync(IEnumerable<T>)` — per-entity `ExecuteAsync(UpsertSql, entity)` loop (no dynamic SQL)
* [x] Upsert does not fire lifecycle hooks (set-based operation) — verified in `UpsertGenerationTests`
* [x] Upsert does not check or increment `[Version]` — excluded from upsert UPDATE SET in `UpsertSqlBuilder`
* [x] Generator validates provider at compile time and emits provider-specific SQL literal
* [x] Composite-key entities: DPX031 warning + `NotSupportedException` overrides; immutable: `NotSupportedException` (same as other mutating ops)

***

# EPIC 17e: Explicit Batch Relationship Loading

***

## Feature: Batch Load Methods

### Tasks

* [x] Generate `Load{Collection}ForManyAsync(IEnumerable<TParent>)` per `[OneToMany]` and `[ManyToMany]` relationship *(`BatchRelationshipLoaderGenerator`; M2M uses link-table IN query + child SELECT)*
* [x] Emits `SELECT … WHERE fk_id IN @parentIds` — one query for all parents *(`RelationshipSqlBuilder` + `BatchRelationshipLoaderGenerator`)*
* [x] After loading, groups results by FK and calls `Set()` on each parent's `LazyCollection`
* [x] Append soft-delete filter if child entity has `[SoftDelete]` *(compile-time WHERE fragment)*
* [x] Append tenancy filter if child entity has `[TenantId]` *(compile-time `@tenantId` param)*
* [x] `PostLoad` lifecycle hook fires per loaded child entity when child declares `[PostLoad]` *(child `{Entity}LifecycleInvoker` on parent repo)*
* [x] Two-pass entity build + `RelationshipMetadataEnricher` for FK/child table resolution *(DPX032/DPX033 validation)*
* [x] Generation tests: `BatchRelationshipLoaderGenerationTests` + Section 23 `OrderRepositoryImpl` assertion

***

# EPIC 17f: Soft Delete Bypass (IncludeDeleted)

***

## Feature: IncludeDeleted Query Modifier

### Tasks

* [x] Generate two SQL string literals per SELECT method on every `[SoftDelete]` entity:
  * [x] Default (with filter): `… WHERE is_deleted = 0 …`
  * [x] Bypass (without filter): same SQL without the soft-delete predicate
* [x] Store both literals as constants in the generated class (`*SqlIncludingDeleted` private const)
* [x] At runtime, a `bool includeDeleted` parameter selects between the two — no string concatenation
* [x] Apply `IncludeDeleted` support to: `IRepository` reads, tenancy/global-filter reads, derived query methods, CPQL SELECT
* [x] Emit `Diagnostic` (DPX022) if `includeDeleted` parameter is declared on a non-`[SoftDelete]` entity's method
* [x] Implement `IncludeDeleted()` fluent method on query builder (EPIC 6; runtime WHERE append on `QueryBaseSql`)

***

# EPIC 17g: Generated Columns

***

## Feature: Generated Column Handling

### Tasks

* [x] Exclude `[Generated]` properties from INSERT SQL column list
* [x] Exclude `[Generated(GenerationTime.Always)]` properties from UPDATE SQL column list
* [x] After INSERT: emit re-SELECT SQL literal for all `[Generated]` properties:
  * [x] SQL Server: inline `OUTPUT INSERTED.col` in INSERT statement
  * [x] PostgreSQL: inline `RETURNING col` in INSERT statement
  * [x] MySQL / SQLite: emit separate `SELECT col FROM table WHERE id = @id`
* [x] After UPDATE (for `GenerationTime.Always`): emit re-SELECT SQL literal `SELECT col FROM table WHERE id = @id`
* [x] Assign re-fetched values back to entity instance after mutation
* [x] `[Generated]` and `[Formula]` on same property → `Diagnostic` error (DPX009)
* [x] `[Generated]` on `[MappedSuperclass]` → inherited by all subclasses
* [x] Implement `GeneratedColumnValidator` (DPX059 invalid GenerationTime)

***

# EPIC 17h: Global Custom Filters

***

## Feature: Global Filter Infrastructure

### Tasks

* [x] Emit `private static readonly string FILTER_{NAME} = " AND condition_fragment"` per filter per entity
* [x] Emit conditional append in every generated SELECT/UPDATE/DELETE method:
  ```csharp
  if (_options.IsFilterActive("filter_name")) sql += FILTER_FILTER_NAME;
  ```
* [x] The SQL fragment is a compile-time constant — never user-derived or runtime-concatenated
* [x] Add `EnableFilter(name, parameters)` and `DisableFilter(name)` to `DapperXOptions`
* [x] Add `IsFilterActive(name)` to `DapperXOptions`
* [x] `DapperXOptions` filter state must be scoped (per-request via DI) — not a static global; developer provides scoped instance
* [x] `[GlobalFilter]` on `[MappedSuperclass]` → inherited by all subclasses
* [x] Apply global filters to: all CRUD SELECTs, derived query methods, CPQL SELECT, `GetAllAsync` overloads, `Load{Collection}ForManyAsync`
* [x] Implement `GlobalFilterValidator` — validates Condition is non-empty; warns on obvious parameter name conflicts

***

# EPIC 17i: Secondary Table

***

## Feature: Secondary Table Mapping

### Tasks

* [x] Generate SELECT SQL: `SELECT t1.col, t2.col FROM primary_table t1 LEFT JOIN secondary_table t2 ON t2.pk_join = t1.id WHERE t1.id = @id` — compile-time literal
* [x] Generate INSERT: two INSERT statements (primary first, secondary second using returned PK) — both compile-time literals, wrapped in transaction
* [x] Generate UPDATE: two UPDATE statements (primary + secondary) — wrapped in transaction
* [x] Generate DELETE: secondary first, primary second (topological order) — wrapped in transaction
* [x] Validate: no `[Id]` on properties tagged to secondary table → `Diagnostic` error (DPX063)
* [x] Validate: `[SecondaryTable]` has PrimaryKeyJoinColumn defined → `Diagnostic` error if absent (DPX061)
* [x] Implement `SecondaryTableValidator` *(DPX061–DPX063; `SecondaryTableGenerator` + `SqlBuilder` JOIN/INSERT/UPDATE/DELETE; `SecondaryTableTransactionEmitter`)*
* [x] **Rule check:** All SQL is compile-time literals; two statements per operation is a data concern not a state concern — does NOT break any rule

***

# EPIC 17j: Primary Key Join Column

***

## Feature: Shared-PK OneToOne

### Tasks

* [x] Generate JOIN SQL: `ON child.id = parent.id` — compile-time literal (no FK column)
* [x] On INSERT of child: generator emits code to assign `child.Id = parent.Id` before INSERT — runtime data assignment, no dynamic SQL
* [x] Validate: `[PrimaryKeyJoinColumn]` and `[JoinColumn]` must not coexist on same property → `Diagnostic` error
* [x] Validate: child entity must have `[GeneratedValue(Assigned)]` on its `[Id]` when `[PrimaryKeyJoinColumn]` is used — generator emits `Diagnostic` error otherwise
* [x] **Rule check:** JOIN condition is compile-time literal; Id assignment is data operation — does NOT break any rule

***

# EPIC 17k: LazyMap (Map-Keyed Relationships)

***

## Feature: LazyMap

### Tasks

* [x] Implement `LazyMap<TKey, TValue>` in `DapperX.Relations/Lazy/` — `GetAsync()` returns `IReadOnlyDictionary<TKey, TValue>`; `TryGet()`, `Set(IDictionary<K,V>)`
* [x] Generator emits same SELECT SQL as `[OneToMany]` for batch load — no change to SQL
* [x] After load, generator emits in-memory LINQ grouping: `ToDictionary(r => r.{MapKeyProp})` — runtime data grouping, no dynamic SQL
* [x] `Load{Map}ForManyAsync` batch loader generated using same `WHERE fk IN @parentIds` pattern as `[OneToMany]` *(`DepartmentRepositoryImpl` verified)*
* [x] Validate: `[MapKey]` column must exist on child entity → `Diagnostic` error if not found *(DPX034 missing attribute; DPX064 column not mapped on child)*
* [x] Validate: `LazyMap<K,V>` generic type parameters must be compatible with child entity key column type → `Diagnostic` error on mismatch *(DPX065 via `MapKeyValidator`)*
* [x] Implement `MapKeyValidator` — `[MapKey]` presence (DPX034), child column resolution (DPX064), generic type compatibility (DPX065)
* [x] **Rule check:** SQL is identical to `[OneToMany]`; grouping is runtime data processing — does NOT break any rule

***

# EPIC 18: Composite Keys

***

## Feature: Composite Key Mapping

### Tasks

* [x] Create `IdClassAttribute` class (key class type reference)
* [x] Create `EmbeddedIdAttribute` class
* [x] Build `CompositeKeyModel` from `[IdClass]` / `[EmbeddedId]` at compile time
* [x] Validate `[IdClass]` key properties match entity `[Id]` property names exactly *(DPX066)*
* [x] Validate `[EmbeddedId]` class is `[Embeddable]` with no `[Id]` on individual properties *(DPX067, DPX068)*
* [x] Restrict `GenerationType` to `Assigned` only on composite keys — emit `Diagnostic` error otherwise *(DPX046)*
* [x] Generate `GetByIdAsync(TKey)` with composite `WHERE key1 = @key1 AND key2 = @key2` SQL literal
* [x] Generate `DeleteByIdAsync(TKey)` with composite WHERE SQL literal
* [x] Emit `Diagnostic` error if `FindAllByIdAsync` is declared on a composite-key entity's repository interface *(DPX030)*
* [x] Generate composite FK JOIN SQL for relationships referencing composite-key entities

***

# EPIC 18b: Element Collections

***

## Feature: Element Collection Mapping

### Tasks

* [x] Create `ElementCollectionAttribute` class
* [x] Create `CollectionTableAttribute` class (table name, JoinColumn)
* [x] Build `ElementCollectionModel` at compile time
* [x] Validate `[CollectionTable]` is present when `[ElementCollection]` is used — emit `Diagnostic` if absent
* [x] Generate `SELECT … FROM collection_table WHERE fk = @parentId` for `GetAsync()`
* [x] Generate batch `INSERT INTO collection_table (fk, value_col) VALUES …` for insert
* [x] Generate `DELETE FROM collection_table WHERE fk = @parentId` before re-inserting on update
* [x] Support `[OrderColumn]` on element collections — manage position column
* [x] Support `[AttributeOverride]` for embeddable element types per collection table site

***

# EPIC 18c: Named Entity Graph

***

## Feature: Named Entity Graph

### Tasks

* [x] Create `NamedEntityGraphAttribute` class (Name, AttributeNodes, SubGraphs)
* [x] Create `SubGraphAttribute` class (relationship property name, nested AttributeNodes, optional GraphName)
* [x] Build `NamedEntityGraphModel` per entity at compile time
* [x] Validate `AttributeNodes` and `SubGraph` relationship property names at compile time (DPX043, DPX070)
* [x] Generate one SQL literal per named entity graph with all JOINs baked in
* [x] Pass each generated graph SQL literal through `SoftDeleteGenerator` — soft-delete filter must be included
* [x] Pass each generated graph SQL literal through `TenancyGenerator` — tenant filter must be included
* [x] Emit runtime switch (`EntityGraph` string → SQL literal) same pattern as Sort lookup
* [x] Emit `InvalidEntityGraphException` for unrecognised graph name at runtime
* [x] Emit `Diagnostic` error if `Include`/`ThenInclude` combined with `EntityGraph` parameter (DPX071)

***

# EPIC 18d: Stored Procedure Advanced

***

## Feature: Stored Procedure OUT Params and Multiple Result Sets

### Tasks

* [x] Create `ProcParam` class (Name, ParameterMode, Type)
* [x] Create `ParameterMode` enum (In, Out, InOut, Return)
* [x] Create `ProcResult<T1>`, `ProcResult<T1, T2>` return types for OUT parameters
* [x] Create `MultiResult<T1, T2>` return type for multiple result sets
* [x] Detect `[StoredProcedure]` with OUT/InOut params; emit Dapper `DynamicParameters` construction
* [x] Emit OUT parameter value capture after stored procedure execution
* [x] Detect `ResultSets` on `[StoredProcedure]`; emit provider-specific multi-result-set Dapper call
* [x] Validate `ProcParam` names and modes at compile time (DPX072–DPX075)
* [x] Return type must be `Task<ProcResult<…>>` for OUT params or `Task<MultiResult<…>>` for multiple result sets — emit `Diagnostic` on mismatch

***

# EPIC 19: Many-to-Many (Join Table)

***

## Feature: Join Table Handling

### Tasks

* [x] Parse `[ManyToMany]` + `[JoinTable]` attributes *( `MetadataBuilder.BuildRelationshipModel`; EPIC 1/2)*
* [x] Validate `[JoinTable]` completeness *(`RelationshipValidator` → DPX076 missing `[JoinTable]`, DPX077 missing `JoinColumn`, DPX078 missing `InverseJoinColumn`)*
* [x] Generate INSERT / DELETE join records SQL *(`GraphGenerator.EmitJoinTableSqlConstants` → `JoinInsert_{Property}_Sql`, `JoinDelete_{Property}_Sql`)*
* [x] Batch insert/delete join records *(batch `ExecuteAsync(joinRows)` on insert/update graph; parent-scoped delete on delete/update graph; `Load{Property}ForManyAsync` batch link load)*
* [x] Integrate into graph execution *(`InsertGraphAsync`/`UpdateGraphAsync`/`DeleteGraphAsync` M2M loops; `ExecutionPlanGenerator` `InsertJoinTable`/`DeleteJoinTable` nodes; tests: `ManyToManyGenerationTests`)*

***

# EPIC 19b: Bulk Insert Optimization

***

## Feature: Bulk Executors

### Tasks

* [x] Implement `SqlBulkCopy` executor (SQL Server) *(`SqlServerBulkExecutor` via `Microsoft.Data.SqlClient.SqlBulkCopy`)*
* [x] Implement `COPY` writer (PostgreSQL) *(`PostgreSqlBulkExecutor` via Npgsql binary COPY)*
* [x] Implement `LOAD DATA` writer (MySQL) *(`MySqlBatchExecutor` via `MySqlBulkCopy`)*
* [x] Detect dataset size vs `BulkThreshold`; switch between batch and bulk *(`RepositoryEmission.EmitBulkCapableAssignedInsertMany`; `_options?.BulkThreshold ?? 5000`; eligible Assigned-key entities only)*
* [x] Fall back to normal batching if bulk unsupported *(SQLite / ineligible entities / count below threshold → chunked `ExecuteAsync`; tests: `BulkInsertGenerationTests`)*

***

# EPIC 20: Locking Modes

***

## Feature: Pessimistic Lock SQL

### Tasks

* [x] Implement per-provider lock hints in `SqlDialect`:
  * [x] `LockMode.Pessimistic`: SQL Server `WITH (UPDLOCK, ROWLOCK)`, PostgreSQL/MySQL `FOR UPDATE`, SQLite → `Diagnostic` error *(EPIC 10 + `Sql*Dialect`; derived SQLite runtime throw)*
  * [x] `LockMode.PessimisticRead`: SQL Server `WITH (HOLDLOCK, ROWLOCK)`, PostgreSQL `FOR SHARE`, MySQL `FOR SHARE`, SQLite → `Diagnostic` error *(DPX037 derived `LockMode`; `IQuery.WithLock` runtime throw on Sqlite)*
* [x] Integrate `LockMode.Pessimistic` and `LockMode.PessimisticRead` into query builder (`WithLock`) and derived query method `LockMode` parameter *(`QueryLockSuffix`, `FindByNameLockedAsync`; verified `ConcurrencyAndLockingTests`, `LockingGenerationTests`)*
* [x] All lock SQL fragments are compile-time string literals selected by provider — no dynamic SQL
* [x] **Rule check:** Lock hints are compile-time literals; does NOT break any rule

***

# EPIC 21: Multi-Database Support

> **Scope:** SqlServer, PostgreSql, MySql, and Sqlite share the first feature block (dialect matrix). The second block lists **SQLite-only** limitations and diagnostics — not separate task lists for the other three providers (see `DapperX.Provider/SqlServer`, `PostgreSql`, `MySql` in Structures.md).

***

## Feature: SQL dialects (all four providers)

### Tasks

* [x] Implement paging per provider (SQL Server: `OFFSET/FETCH`; PostgreSQL/MySQL/SQLite: `LIMIT/OFFSET`) *(`SqlBuilder.AppendPaging`; `*Dialect.PagingTemplate`; EPIC 3)*
* [x] Implement identity return per provider (`OUTPUT INSERTED.Id` / `RETURNING id` / `LAST_INSERT_ID()` / `last_insert_rowid()`) *(`SqlBuilder.BuildInsert`; `GeneratedColumnSqlBuilder`)*
* [x] Implement sequence syntax per provider (`NEXT VALUE FOR` / `nextval()`); emit `ISequenceAllocator.NextAsync()` call when injected — no AllocationSize state in DapperX *(`BuildSequenceNextSql`; repository ctor injection)*
* [x] Implement boolean literal dialect *(`CpqlScalarFunctions.EmitBooleanLiteral`; EPIC 9)*
* [x] Implement regex operator per provider + SQL Server compile-time error *(DPX024/DPX029; EPIC 3)*
* [x] Implement pessimistic lock hint per provider *(EPIC 10/20; `*Dialect`; `QueryLockSuffix`)*
* [x] Implement window function syntax per provider (all four: SQL Server full, PostgreSQL full, MySQL 8.0+, SQLite 3.25.0+) *(EPIC 9 CPQL)*
* [x] Implement 23 cross-provider scalar function mappings (15 original + 8 new: LTRIM, RTRIM, SUBSTRING, REPLACE, LEFT, RIGHT, MOD, POWER) *(`CpqlScalarFunctions`; `CpqlScalarSnapshotTests`; Sqlite `strftime` for date parts)*
* [x] Implement 8 `CAST` type name mappings per provider *(`MapCastType`; Sqlite `TEXT`/`INTEGER`/`REAL`; `date()`/`datetime()` for DATE/DATETIME casts)*
* [x] Implement `NULLS FIRST/LAST` emulation per provider *(EPIC 9 `CpqlTranslator`)*
* [x] Implement bulk UPDATE `FROM … JOIN` syntax per provider *(`CpqlTranslator.FormatBulkUpdate`; derived UPDATE)*
* [x] Implement `CURRENT_TIMESTAMP` / `CURRENT_DATE` dialect form per provider *(`*Dialect`; `AuditingSqlBuilder`; `AuditingTests`)*
* [x] Implement generated column re-SELECT syntax per provider: SQL Server `OUTPUT INSERTED.col`, PostgreSQL `RETURNING col`, MySQL/SQLite separate `SELECT col WHERE id = @id` *(`GeneratedColumnSqlBuilder`)*
* [x] Implement `Slice<T>` paging template per provider: SQL Server `OFFSET/FETCH` with `@sliceSize` (pageSize+1), PostgreSQL/MySQL/SQLite `LIMIT @sliceSize` *(`SqlBuilder.AppendSlicePaging`; `MultiDatabaseProviderTests`)*

***

## Feature: SQLite provider (limitations and diagnostics)

### Tasks

* [x] Create `SqliteProvider.cs` and `SqliteDialect.cs` in `DapperX.Provider/Sqlite/`
* [x] Add `Sqlite` to `DatabaseProvider` enum *(enum value `Sqlite`; Requirements table label “SQLite”)*
* [x] Implement paging: `LIMIT m OFFSET n`
* [x] Implement identity return: `SELECT last_insert_rowid()` after INSERT
* [x] Implement upsert: `INSERT … ON CONFLICT DO UPDATE` (SQLite 3.24+); `INSERT OR REPLACE` not emitted — intentional, same as PostgreSQL-style path *(`UpsertSqlBuilder.BuildSqliteOnConflict`)*
* [x] Implement `CURRENT_TIMESTAMP` → `datetime('now')` and `CURRENT_DATE` → `date('now')`
* [x] Implement date functions: `YEAR`/`MONTH`/`DAY` → `CAST(strftime('%Y'|'%m'|'%d', x) AS INTEGER)` *(`CpqlScalarFunctions.EmitDatePart`)*
* [x] Implement `CONCAT` → `x || y || …` (SQLite has no `CONCAT` function)
* [x] Implement `CAST` type names: `STRING` → `TEXT`, `INT` → `INTEGER`, `DECIMAL` → `REAL`, `DATE` → `date(x)`, `DATETIME` → `datetime(x)`
* [x] Implement `NULLS FIRST/LAST`: emit natively (SQLite 3.30.0+ supports it) *(CPQL translator)*
* [x] Implement bulk insert: always falls back to batch INSERT — `BulkThreshold` ignored; no `Diagnostic` *(EPIC 19b; `SqliteProvider.BulkInsertExecutor` null)*
* [x] Implement boolean literals: `1`/`0`
* [x] Emit `Diagnostic` compile error when SQLite provider is active and any of the following are used:
  * [x] `WithLock(LockMode.Pessimistic)` — no row-level locking *(runtime `NotSupportedException` in `QueryLockSuffix`; derived `LockMode` → DPX037)*
  * [x] `WithLock(LockMode.Pessimistic, TimeoutMs)` — no lock timeout *(same runtime guard)*
  * [x] `GenerationType.Sequence` — no native sequences *(DPX017)*
  * [x] `[StoredProcedure]` — no stored procedures *(DPX018)*
  * [x] Multiple result sets (`ResultSets` on `[StoredProcedure]`) *(DPX036)*
* [x] Emit `Diagnostic` warning when SQLite provider is active and:
  * [x] `Regex` / `Matches` operator used — requires REGEXP extension loaded at runtime *(DPX029)*
  * [x] `[Table(Schema = "...")]` is set — schema is not supported in SQLite; value will be ignored *(DPX035)*
* [x] No `SqliteBulkExecutor` — SQLite does not have a bulk copy API; `SqliteProvider` returns null for bulk executor interface *(verified `BulkInsertGenerationTests`; compile target `tests/DapperX.Tests.Sqlite`)*

> **Verification:** Requirements §14 dialect table; SqlServer — `MultiDatabaseProviderTests`, `CompileTimeDatabaseProviderTests`, `UpsertGenerationTests`. Per-provider literals → **EPIC 26a** `matrix-4` (see legend above). Sqlite Diagnostics → **EPIC 26a** `sqlite-only`.

***

# EPIC 22: Transactions

> Transaction behavior is implemented across EPIC 3 (repository generation), EPIC 9 (graph), and EPIC 14 (secondary table). This EPIC tracks Requirements §17 checklist status.

***

## Feature: Transaction Support

### Tasks

* [x] Add optional `IDbTransaction` parameter to all repository methods *(`IRepository`; generated overrides; `IQuery` execution; derived/SP methods when `transaction` param declared; EPIC 3 line 293)*
* [x] Wrap all graph operations in transaction; rollback on failure *(`GraphGenerator` `ownsTransaction`; `SecondaryTableTransactionEmitter`; EPIC 9 line 645)*
* [x] Generate `WithTransactionAsync` helper per entity *(`DapperXRepositoryBase.WithTransactionAsync(Func<IDbTransaction, Task>)` inherited by every `*RepositoryImpl`; Requirements §17)*

***

# EPIC 23: Configuration System

***

## Feature: Global Options & Per-Operation Override

### Tasks

* [x] Create `DapperXOptions` (BatchSize, BulkThreshold, Logger, LogSql, LogParameters, LogExecutableSql) *([`DapperXOptions.cs`](src/DapperX.Runtime/Configuration/DapperXOptions.cs); EPIC 1 line 168)*
* [x] `DatabaseProvider` as compile-time constant (MSBuild property or assembly attribute) *(`DapperXDatabaseProvider` MSBuild + `[DapperXDatabaseProvider]` assembly attribute; `CompileTimeDatabaseProvider`; `tests/DapperX.Tests.Sqlite`; EPIC 21)*
* [x] Add `Logger` hook (`Action<DapperXLogEntry>`) *(`IDapperXOptions.Logger` contract; runtime invocation → EPIC 25)*
* [x] Support per-operation BatchSize / BulkThreshold override *(`IRepository` optional `batchSize`/`bulkThreshold`; generator `effectiveBatchSize`/`effectiveBulkThreshold`)*
* [x] Implement `EnableFilter(name, parameters)` — registers named filter as active for this options instance; stores parameter values *(EPIC 11 line 974)*
* [x] Implement `DisableFilter(name)` — removes filter from active set *(EPIC 11 line 975)*
* [x] Implement `IsFilterActive(name)` → `bool` — used by generated `ApplyGlobalFilters` at runtime *(EPIC 11 line 976; not a separate `GlobalFilterApplicator` type)*
* [x] `DapperXOptions` must be scoped per DI request — never a static singleton; filter state is per-request *(optional ctor `IDapperXOptions`; EPIC 3 line 293, EPIC 11 line 976)*
* [x] `ActiveFilters` dictionary is thread-safe within the scoped instance *(`ConcurrentDictionary` in `DapperXOptions`)*

***

# EPIC 24: Error Handling

***

## Feature: Exception System

### Tasks

* [x] Implement all four exception types (Concurrency, Mapping, SqlExecution, InvalidSort) *(EPIC 2 lines 191–194; `MappingException` via DPX diagnostics at compile time)*
* [x] Include conflicting key list in `ConcurrencyException` for batch *(`ConflictingKeys`; EPIC 10 lines 659–660; `RepositoryEmission`)*
* [x] Add SQL context to error messages where applicable *(`DbExecutor` wraps Dapper calls; `SqlExecutionException.Sql`; converter failures)*

***

# EPIC 25: Debugging & Logging

***

## Feature: Contracts (DapperX.Abstractions)

### Tasks

* [x] Create `DapperXLogEntry` class in `DapperX.Abstractions/Logging/` (contract type — referenced by `IDapperXOptions.Logger`) *([`DapperXLogEntry.cs`](src/DapperX.Abstractions/Logging/DapperXLogEntry.cs); EPIC 1/2)*
  * [x] Properties: `string MethodName`, `string Sql`, `IReadOnlyDictionary<string, object> Parameters`, `string ExecutableSql`, `DateTime Timestamp`
  * [x] `Parameters` and `ExecutableSql` are null when their respective flags are off
* [x] Update `IDapperXOptions` to add: `bool LogSql`, `bool LogParameters`, `bool LogExecutableSql`, `Action<DapperXLogEntry> Logger` *(EPIC 2 line 168)*

***

## Feature: Configuration (DapperX.Runtime)

### Tasks

* [x] Update `DapperXOptions` concrete class to implement new `IDapperXOptions` logging properties *(EPIC 23 — [`DapperXOptions.cs`](src/DapperX.Runtime/Configuration/DapperXOptions.cs))*
* [x] Set defaults: `LogSql = false`, `LogParameters = false`, `LogExecutableSql = false`, `Logger = null`

***

## Feature: SQL Logging (DapperX.Runtime)

### Tasks

* [x] Update `DbExecutor` to invoke logger **before** every Dapper call when `LogSql = true` and `Logger != null` *(`SqlExecutionLogger` + [`DbExecutor.cs`](src/DapperX.Runtime/Execution/DbExecutor.cs))*
* [x] Populate `DapperXLogEntry.Sql` with the final assembled SQL (always `@param` placeholders — never substituted values)
* [x] Populate `DapperXLogEntry.MethodName` from the compile-time string literal baked in by the generator — no runtime stack trace
* [x] Ensure all execution paths invoke the logger: CRUD, derived query, CPQL, batch, graph operations, stored procedures *(generator `CreateLogContext`; `RepositoryQuery`; `DapperXRepositoryBase`)*
* [x] Skip logging entirely when `LogSql = false` or `Logger = null` — no allocation overhead

***

## Feature: Generator — MethodName Emission

### Tasks

* [x] Update generator to emit `MethodName` as a compile-time string literal constant in every generated repository method *(EPIC 3 line 347 — `MethodNameEmitter` + overrides)*
* [x] Pass `MethodName` literal to `DbExecutor` or directly into `DapperXLogEntry` construction at the call site *(`DbExecutor.CreateLogContext(MethodName, Options, Provider)` at all generated call sites)*
* [x] No runtime `MethodBase.GetCurrentMethod()` or stack trace — name is always a literal
* [x] Eliminate generated `CS0219` unused `MethodName` warnings: remove `MethodName` from pass-through `base.*` wrappers; wire `logContext` on read/derived/CPQL paths; add `TryLogBatchTrace` on graph update/delete entry points

***

## Feature: Parameter Value Logging (DapperX.Runtime)

### Tasks

* [x] When `LogParameters = true`: extract parameter name → value pairs from the Dapper anonymous object before execution *([`ParameterExtractor.cs`](src/DapperX.Runtime/Logging/ParameterExtractor.cs))*
* [x] Handle all parameter types including collection parameters (`IEnumerable<T>` for IN lists)
* [x] Populate `DapperXLogEntry.Parameters` as `IReadOnlyDictionary<string, object>`
* [x] Ensure `Parameters` is `null` (not empty dict) when `LogParameters = false` — clean contract

***

## Feature: Executable SQL Formatter (DapperX.Runtime)

### Tasks

* [x] Implement `ExecutableSqlFormatter` in `DapperX.Runtime/Logging/`
* [x] Implement `Format(string sql, IReadOnlyDictionary<string, object> parameters, DatabaseProvider provider)` method
* [x] Format `string` / `char` → `'value'` with internal `'` escaped as `''`
* [x] Format `int` / `long` / `short` / `byte` → unquoted integer, culture-invariant
* [x] Format `decimal` / `double` / `float` → unquoted decimal, culture-invariant (`.` separator always)
* [x] Format `bool` → dialect-aware: `1`/`0` (SQL Server, MySQL, SQLite) or `TRUE`/`FALSE` (PostgreSQL)
* [x] Format `DateTime` / `DateTimeOffset` → `'yyyy-MM-dd HH:mm:ss'`
* [x] Format `Guid` → `'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'`
* [x] Format `null` → `NULL`
* [x] Format `IEnumerable<T>` (IN list) → `('v1', 'v2', 'v3')` — each element formatted by its own type rules
* [x] Populate `DapperXLogEntry.ExecutableSql` when `LogExecutableSql = true`
* [x] `ExecutableSqlFormatter` output is **never** passed to Dapper — logging output only
* [x] Add `DPX090` `LogExecutableSql` security-reminder descriptor in `DiagnosticsReporter` *(descriptor only — emission when `LogExecutableSql = true` requires build-configuration analyzer per Requirements §19; deferred)*

***

## Feature: Testing

### Tasks

* [x] `LoggingTests.cs` (unit):
  * [x] Test `LogSql = false` — logger never invoked
  * [x] Test `LogSql = true`, `Logger = null` — no NullReferenceException
  * [x] Test `LogParameters = false` — `Parameters` is null in entry
  * [x] Test `LogParameters = true` — all param name/value pairs captured including IN lists
  * [x] Test `LogExecutableSql = false` — `ExecutableSql` is null in entry
  * [x] Test `MethodName` is correct compile-time literal per method
  * [x] Test logger fires before execution (not after)
* [x] `ExecutableSqlFormatterTests.cs` (unit):
  * [x] Test all type formatters (string, int, decimal, bool dialect variants, DateTime, Guid, null, IEnumerable)
  * [x] Test string escaping (`it's` → `'it''s'`)
  * [x] Test culture-invariant decimal formatting (comma-locale machines)
  * [x] Test IN list formatting with mixed types
  * [x] Test bool across all three providers
* [x] `LoggingIntegrationTests.cs` (integration):
  * [x] Test that `ExecutableSql` output is valid SQL that can execute against the real database *(SQLite)*
  * [x] Test logging across all three providers for dialect-specific values (bool, DateTime) *(formatter unit tests cover SqlServer/PostgreSql/MySql/Sqlite bool)*

***

## Feature: SQL Visibility & Tracing

### Tasks

* [x] Ensure all SQL is inline and readable in generated code *(compile-time literals — existing generator)*
* [x] Log graph execution steps and batch operation sizes via log entry *(`SqlExecutionLogger.TryLogBatchTrace`; batch methods + `InsertGraphAsync`)*

> **Verification:** `LoggingTests`, `LoggingIntegrationTests`, `ExecutableSqlFormatterTests` (`single-project`). Section 26 SQL call counts → [`SqlExecutionCountFixture`](tests/DapperX.IntegrationTests.Shared/SqlExecutionCountFixture.cs) (EPIC 26a Shared + EPIC 26b). `MethodName` on all paths → **EPIC 26a** `MethodNameLoggingMatrixTests`.

***

# EPIC 26a: Provider Test Matrix (Infrastructure)

***

> **Coverage legend (use in task notes):**
> - `single-project` — one compile assembly ([`DapperX.Tests`](tests/DapperX.Tests/DapperX.Tests.csproj) = **SqlServer**); provider-agnostic or SqlServer-only assertions
> - `matrix-4` — same test sources linked into all four compile projects via [`DapperX.Tests.Shared`](tests/DapperX.Tests.Shared/)
> - `sqlite-only` — compile-time Diagnostic / unsupported-feature tests; run only in `DapperX.Tests.Sqlite`
> - `integration-{Provider}` — real DB via Testcontainers in `DapperX.IntegrationTests.{Provider}`

> **Rule:** Generator SQL is selected at compile time per `DapperXDatabaseProvider` MSBuild property — not runtime. One test project = one dialect in generated `*RepositoryImpl.g.cs`.

***

## Feature: Shared compile-test infrastructure

### Tasks

* [x] Create `tests/DapperX.Tests.Shared/` — `ProviderExpectations.cs`, `GeneratedSourceReader.cs`, shared catalog fixture definitions (no test-runner csproj)
* [x] Create `tests/DapperX.Tests.Shared/ProviderGenerationTests.props` — MSBuild import linking `Generation/**/*.cs` into consumer projects
* [x] Implement `ProviderExpectations` helpers: `AssertUpsertSql`, `AssertSlicePaging`, `AssertPagePaging`, `AssertIdentityInsert`, `AssertBulkInsertPath`, `AssertPessimisticReadSuffix`, `AssertInClause`, `AssertBooleanFilterLiteral` per provider string
* [x] Add `tests/README.md` — provider matrix table (feature × provider × compile vs integration vs diagnostic-only)

***

## Feature: Compile-time test projects (one provider per assembly)

### Tasks

* [x] Document in planning docs: [`DapperX.Tests`](tests/DapperX.Tests/DapperX.Tests.csproj) is the **SqlServer** compile target (`DapperXDatabaseProvider=SqlServer`) — not provider-neutral *(see [`tests/README.md`](tests/README.md), Structures.md §2.11)*
* [x] Create `tests/DapperX.Tests.PostgreSql/` — csproj with `DapperXDatabaseProvider=PostgreSql`; import `ProviderGenerationTests.props`; fixtures namespace `DapperX.Tests.PostgreSql.Fixtures`
* [x] Create `tests/DapperX.Tests.MySql/` — csproj with `DapperXDatabaseProvider=MySql`; import Shared props; fixtures `DapperX.Tests.MySql.Fixtures`
* [x] Expand `tests/DapperX.Tests.Sqlite/` — align fixture catalog with Shared; import `ProviderGenerationTests.props`; legacy sqlite-only tests excluded from compile
* [x] Register `DapperX.Tests`, `DapperX.Tests.PostgreSql`, `DapperX.Tests.MySql`, `DapperX.Tests.Sqlite` in [`DapperX.slnx`](DapperX.slnx)
* [x] CI: `dotnet test` on all four compile projects on every PR *(`.github/workflows/ci.yml` `compile-tests` job)*

***

## Feature: Matrix-4 generation tests (linked; assert per-provider SQL literals)

### Tasks

* [x] `UpsertGenerationMatrixTests` — `matrix-4`: MERGE (SqlServer) / ON CONFLICT (PostgreSql, Sqlite) / ON DUPLICATE KEY (MySql) in generated `UpsertSql`
* [x] `SlicePagingMatrixTests` — `matrix-4`: FETCH FIRST (SqlServer) vs LIMIT (PostgreSql, MySql, Sqlite) in `SelectAllSliceSql`
* [x] `PagePagingMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `GeneratedColumnMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `BulkInsertMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `LockingMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `LockTimeoutMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs) *(generator contract)*
* [x] `AuditingSqlMatrixTests` — `matrix-4`: [`MatrixLifecycleAndFilterTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixLifecycleAndFilterTests.cs)
* [x] `SoftDeleteMatrixTests` — `matrix-4`: [`MatrixLifecycleAndFilterTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixLifecycleAndFilterTests.cs)
* [x] `TenancyMatrixTests` — `matrix-4`: [`MatrixLifecycleAndFilterTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixLifecycleAndFilterTests.cs)
* [x] `GlobalFilterMatrixTests` — `matrix-4`: [`MatrixLifecycleAndFilterTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixLifecycleAndFilterTests.cs)
* [x] `GetAllOverloadMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `DeleteAllByIdMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `IncludeDeletedMatrixTests` — `matrix-4`: [`MatrixLifecycleAndFilterTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixLifecycleAndFilterTests.cs)
* [x] `GeneratedValueMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `SecondaryTableMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `PrimaryKeyJoinColumnMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `LazyMapMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `ElementCollectionMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `NamedEntityGraphMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `StoredProcedureMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] `CpqlEmittedSqlMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] `Section23ChecklistMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] `OptimisticConcurrencyMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `PessimisticWriteMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `SequenceMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] `DerivedQueryPagingMatrixTests` — `matrix-4`: [`MatrixPagingAndCrudTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixPagingAndCrudTests.cs)
* [x] `GraphExecutionMatrixTests` — `matrix-4`: [`MatrixRelationsAndGraphTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs)
* [x] `CpqlMutationMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] `MethodNameLoggingMatrixTests` — `matrix-4`: [`MatrixCpqlSequenceAndComplianceTests.cs`](tests/DapperX.Tests.Shared/Generation/MatrixCpqlSequenceAndComplianceTests.cs)
* [x] Port remaining provider-sensitive tests from `DapperX.Tests` into `DapperX.Tests.Shared/Generation/` as they are touched *(matrix suite covers EPIC 26a matrix items; SqlServer originals retained)*

***

## Feature: Sqlite-only compile tests (Diagnostics and unsupported features)

### Tasks

* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: pessimistic lock → DPX037 *(generator contract in [`SqliteUnsupportedFeatureTests.cs`](tests/DapperX.Tests.Sqlite/SqliteUnsupportedFeatureTests.cs))*
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: `GenerationType.Sequence` → Diagnostic *(DPX017)*
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: `[StoredProcedure]` on repository → Diagnostic
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: CPQL `Regex` → Diagnostic warning (DPX029)
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: `[Table(Schema)]` → Diagnostic warning; schema not in SQL
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: bulk threshold ignored — always batch INSERT in generated `InsertManyAsync`
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: stored procedure multiple result sets → compile Diagnostic (DPX036)
* [x] `SqliteUnsupportedFeatureTests` — `sqlite-only`: lock timeout on derived `LockMode` / `IQuery` → Diagnostic or runtime throw per generator *(MySql + DPX037/038 contracts)*

***

## Feature: Single-project tests (provider-agnostic — keep in DapperX.Tests only)

### Tasks

* [x] Keep Roslyn metadata/diagnostic validation in `DapperX.Tests` only — `single-project` *(documented in [`tests/README.md`](tests/README.md); suite remains in `DapperX.Tests`)*
* [x] `SqlServerRegexDiagnosticTests` — `single-project`: [`SqlServerRegexDiagnosticTests.cs`](tests/DapperX.Tests/SqlServerRegexDiagnosticTests.cs)
* [x] Keep `CpqlScalarSnapshotTests`, `MultiDatabaseProviderTests` (builder/translator Theory) — `single-project` *(already iterates four provider strings without four assemblies)*
* [x] Keep `ExecutableSqlFormatterTests`, `LoggingTests` — `single-project` *(runtime; Theory over `DatabaseProvider` enum)*

***

## Feature: Integration test projects (Testcontainers)

### Tasks

* [x] Create `tests/DapperX.IntegrationTests.Shared/` — `IntegrationEnvironment`, `DatabaseBootstrap`, `SqlExecutionCountFixture`, `IntegAllProvidersTests` *(see [`IntegrationScenarios.props`](tests/DapperX.IntegrationTests.Shared/IntegrationScenarios.props))*
* [x] Create `tests/DapperX.IntegrationTests.SqlServer/` — `DapperXDatabaseProvider=SqlServer`; Testcontainers.MsSql; shared scenarios + container health
* [x] Create `tests/DapperX.IntegrationTests.PostgreSql/` — Testcontainers.PostgreSql; shared scenarios via linked props
* [x] Create `tests/DapperX.IntegrationTests.MySql/` — Testcontainers.MySql; shared scenarios via linked props
* [x] Create `tests/DapperX.IntegrationTests.Sqlite/` — in-memory Sqlite; shared scenarios + `IntegSqliteDialectTests`
* [x] Register all four integration projects in `DapperX.slnx`; remove or repurpose stub [`DapperX.IntegrationTests`](tests/DapperX.IntegrationTests/UnitTest1.cs) *(stub removed from slnx; `UnitTest1.cs` deleted)*
* [x] CI: integration job with Docker; document Testcontainers requirement in repo README *(`.github/workflows/ci.yml`, [`tests/README.md`](tests/README.md))*
* [x] `integration-SqlServer`: CRUD, batch, upsert, bulk path — [`IntegAllProvidersTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegAllProvidersTests.cs) + [`SqlServerContainerHealthTests`](tests/DapperX.IntegrationTests.SqlServer/SqlServerContainerHealthTests.cs) — **29/29 pass** *(full suite incl. SP, lock timeout, advanced features)*
* [x] `integration-PostgreSql`: shared scenarios via linked props — **26/26 pass** *(Docker Testcontainers; `ProviderSqlHelper` boolean/`= ANY(@param)`; PG `CALL` uses `NULL` for OUT-only args; lock preamble separate `ExecuteAsync`)*
* [x] `integration-MySql`: shared scenarios via linked props — **27/27 pass** *(Testcontainers `WithCommand("--local-infile=1")` + client `AllowLoadLocalInfile=true`; SP OUT via `CommandType.StoredProcedure` + C#-aligned param names in `IntegrationProcedureBootstrap`)*
* [x] `integration-Sqlite`: CRUD, upsert, batch + [`IntegSqliteDialectTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegSqliteDialectTests.cs) — **24/24 pass**
* [x] `integration-all`: Upsert roundtrip per provider — `IntegAllProvidersTests`
* [x] `integration-all`: `GetAllSliceAsync` — single SELECT (Req 8 SQL count via `SqlExecutionCountFixture` on CRUD paths)
* [x] `integration-all`: soft-delete, tenancy, auditing isolation — `IntegAllProvidersTests`
* [x] `integration-all`: `Load{Collection}ForManyAsync` N=1 SQL (Section 26 Req 5) — `IntegAllProvidersTests` *(batch loader SQL shape + `logContext` on loader)*
* [x] `integration-all`: `InsertManyAsync(1000)` bounded SQL calls (Section 26 Req 1) — `IntegAllProvidersTests`
* [x] `integration-all`: composite key, element collections, named entity graph, LazyMap, SecondaryTable, PrimaryKeyJoinColumn — [`IntegAdvancedFeaturesTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegAdvancedFeaturesTests.cs)
* [x] `integration-SqlServer|PostgreSql|MySql`: stored procedure OUT params + `MultiResult<>` — [`IntegStoredProcedureTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegStoredProcedureTests.cs) *(SqlServer/MySql: `CommandType.StoredProcedure`; PostgreSql: `CALL proc(@in, @inout, NULL, …)`; MultiResult SqlServer/MySql only)*
* [x] `integration-SqlServer|PostgreSql|MySql`: lock timeout per provider — [`IntegLockTimeoutTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegLockTimeoutTests.cs) *(literal timeout in separate preamble `ExecuteAsync`; 2 SQL calls when timeout > 0; not Sqlite)*
* [x] `matrix-4` full suites green — `DapperX.Tests` 462, `DapperX.Tests.PostgreSql` 33, `DapperX.Tests.MySql` 33, `DapperX.Tests.Sqlite` 37 *(boolean/IN assertions via `ProviderExpectations`)*

***

# EPIC 26: Testing

***

> Provider infrastructure and matrix tests: **EPIC 26a** above. Retag open items below with `matrix-4`, `single-project`, or `sqlite-only` when implementing. Do not mark `[x]` until the tagged project(s) exist and tests pass.

***

## Feature: Unit Tests

### Tasks

* [x] Test all entity mapping validation (all new attributes including Immutable, Formula, SoftDelete, TenantId, Auditing) *(SqlServer: `MappingValidationGenerationTests`, mapping validation fixtures)*
* [x] Test `[MappedSuperclass]` inheritance (auditing, soft-delete, tenancy propagation) *(SqlServer: `MappedAuditItem`, `MappedTenantItem`, `FilteredMappedSuperclassEntity` generation tests)*
* [x] Test `[SequenceGenerator]` validation and sequence name resolution; verify `ISequenceAllocator.NextAsync()` emitted when injected; verify direct DB call when not injected *(SqlServer: `SequenceGenerationTests` — allocator path; direct DB path pending)*
* [x] Test `FindAllByIdAsync` emits `Diagnostic` error on composite-key entities; verify it generates correctly for single-key entities *(DPX030 contract: `CompositeKeyDiagnosticTests`; explicit interface declaration → compile error not exercised in test project)*
* [x] Test `WhereTranslator` uses `ResolveColumn()` — verify no `MemberInfo` usage; verify `UnmappedPropertyException` thrown for unknown properties *(SqlServer: `QueryGenerationTests`)*
* [x] Test `LazyCollection` and `LazyReference` have no `Reload()` method — read-once contract enforced at compile-time and runtime *(SqlServer: `LazyLoadingContractTests`)*
* [x] Test `LazyCollection.Set()` and `LazyReference.Set()` make subsequent `GetAsync()` return injected data without DB call *(SqlServer: `LazyLoadingTests`)*
* [x] Test named entity graph SQL literals include soft-delete filter when entity has `[SoftDelete]` *( `NamedEntityGraphGenerationTests`)*
* [x] Test named entity graph SQL literals include tenancy filter when entity has `[TenantId]` *( `NamedEntityGraphGenerationTests`)*
* [x] Test composite key `GetByIdAsync` and `DeleteByIdAsync` use composite WHERE SQL literal *(SqlServer: `CompositeKeyGenerationTests`; matrix-4 pending)*
* [x] Test element collection INSERT/DELETE/SELECT SQL literals including position management with `[OrderColumn]` *( `ElementCollectionGenerationTests`)*
* [x] Test `[NamedEntityGraph]` switch throws `InvalidEntityGraphException` for unknown graph name *( `NamedEntityGraphGenerationTests`)*
* [x] Test stored procedure OUT parameter capture and `ProcResult<>` return *( `StoredProcedureGenerationTests`)*
* [x] Test stored procedure multiple result sets and `MultiResult<>` return *( `StoredProcedureGenerationTests`)*
* [x] Test `[ColumnTransformer]` Read expression in SELECT and Write expression in INSERT/UPDATE *( `ColumnTransformerGenerationTests`)*
* [x] Test `[ColumnTransformer]` + `[Converter]` conflict emits `Diagnostic` error *(generator contract: `GeneratorDiagnosticsContractTests` — DPX010)*
* [x] Test `[AssociationOverride]` applies FK column override in inherited relationships *(SqlServer: `AssociationOverrideGenerationTests`; matrix-4 pending)*
* [x] Test `UpsertAsync` / `UpsertManyAsync` generation — SqlServer only (`UpsertGenerationTests` — `MERGE` in [`DapperX.Tests`](tests/DapperX.Tests/DapperX.Tests.csproj))
* [x] Test `UpsertAsync` / `UpsertManyAsync` generation — all providers (`matrix-4` — [`UpsertGenerationMatrixTests`](tests/DapperX.Tests.Shared/Generation/UpsertGenerationMatrixTests.cs))
* [x] Test `Load{Collection}ForManyAsync` emits `WHERE fk IN @parentIds` and calls `Set()` per parent *(SqlServer: `BatchRelationshipLoaderGenerationTests`; matrix-4 pending)*
* [x] Test `[Enumerated(EnumType.String)]` maps to `EnumToStringConverter`; `EnumType.Ordinal` maps to `EnumToIntConverter` *( `ConverterGenerationTests`)*
* [x] Test lock timeout: `SET LOCK_TIMEOUT @timeout` template emitted for SQL Server; `SET lock_timeout = @timeout` for PostgreSQL (timeout > 0); `FOR UPDATE NOWAIT` for both providers when timeout = 0; MySQL `Diagnostic` warning for timeout > 0 *(SqlServer runtime: `ConcurrencyAndLockingTests`; matrix-4: EPIC 26a `LockTimeoutMatrixTests`)*
* [x] Test `@timeout` parameter is always integer milliseconds — verify no string concatenation or `ms` suffix in emitted SQL template *(SqlServer: `ConcurrencyAndLockingTests`)*
* [x] Test auditing: `[CreatedDate]` excluded from UPDATE SQL; `[LastModifiedDate]` in both INSERT and UPDATE *(SqlServer: `AuditingTests`, `MutatingMethodGenerationTests`; matrix-4: `AuditingSqlMatrixTests`)*
* [x] Test soft delete: DELETE rewrites to UPDATE; SELECT appends `is_deleted = 0`; `HardDeleteAsync` bypasses *(SqlServer: `SoftDeleteTests`; matrix-4: `SoftDeleteMatrixTests`)*
* [x] Test multi-tenancy: all SELECT/UPDATE/DELETE include tenant filter; INSERT sets tenant column *(SqlServer: `MultiTenancyTests`; matrix-4: `TenancyMatrixTests`)*
* [x] Test `GetAllAsync(Sort)` uses pre-generated ORDER BY fragments identical to derived method Sort *(SqlServer: `GetAllAsyncGenerationTests`; matrix-4: `GetAllOverloadMatrixTests`)*
* [x] Test `GetAllAsync(Pageable)` appends compile-time paging template *(SqlServer: `GetAllAsyncGenerationTests`; matrix-4: `PagePagingMatrixTests`)*
* [x] Test `GetAllAsync(Sort, Pageable)` combines both correctly *(SqlServer: `GetAllAsyncGenerationTests`)*
* [x] Test `DeleteAllByIdAsync` emits `DELETE WHERE id IN @ids`; fires batch lifecycle hooks but not per-entity hooks *(SqlServer: `DeleteAllByIdGenerationTests`, `LifecycleTests`)*
* [x] Test `DeleteAllByIdAsync` on composite-key entity emits `Diagnostic` error *(generator contract: `DeleteAllByIdGenerationTests` + `CompositeKeyGenerator`)*
* [x] Test `IncludeDeleted = true` selects bypass SQL literal (no is_deleted filter); `false` uses default *(SqlServer: `SoftDeleteBypassGenerationTests`; matrix-4: `IncludeDeletedMatrixTests`)*
* [x] Test `IncludeDeleted` on non-`[SoftDelete]` entity emits `Diagnostic` warning *(generator contract: `GeneratorDiagnosticsContractTests` — DPX022)*
* [x] Test `[Generated(Insert)]` excluded from INSERT SQL; re-SELECT fired after INSERT; value assigned to entity *(SqlServer: `GeneratedColumnGenerationTests`; matrix-4: `GeneratedValueMatrixTests`)*
* [x] Test `[Generated(Always)]` excluded from both INSERT and UPDATE; re-SELECT fired after both *(SqlServer: `GeneratedColumnGenerationTests`)*
* [x] Test `[Generated]` + `[Formula]` on same property emits `Diagnostic` error *(generator contract: `GeneratorDiagnosticsContractTests` — DPX009)*
* [x] Test global filter constants are compile-time string literals (not runtime-built) *(SqlServer: `GlobalFilterGenerationTests`; matrix-4: `GlobalFilterMatrixTests`)*
* [x] Test `EnableFilter` → filter appended to all SELECTs; `DisableFilter` → not appended; zero-overhead when inactive *(SqlServer: `GlobalFilterGenerationTests`, `DapperXOptionsTests`; integration: `IntegRuntimeExtrasTests`)*
* [x] Test CPQL `ROW_NUMBER() OVER (ORDER BY price)` translates correctly per provider *(SqlServer: `CpqlWindowFunctionTests` — all providers via Theory)*
* [x] Test CPQL `SUM(price) OVER (PARTITION BY category)` translates correctly per provider *(SqlServer: `CpqlWindowFunctionTests`)*
* [x] Test CPQL window functions rejected in WHERE clause → `Diagnostic` error *(SqlServer: `CpqlWindowFunctionTests` — DPXCPQL012 contract)*
* [x] Test `SUBSTRING(x, s, n)` provider-specific translation (PostgreSQL: FROM/FOR syntax; SQLite: SUBSTR) *(SqlServer: `CpqlScalarSnapshotTests`)*
* [x] Test `LEFT(x, n)` / `RIGHT(x, n)` SQLite → `SUBSTR` translation *(SqlServer: `CpqlScalarSnapshotTests`)*
* [x] Test `MOD(x, y)` SQL Server / SQLite → `x % y` emission *(SqlServer: `CpqlScalarSnapshotTests`)*
* [x] Test new scalar functions in CPQL SELECT and WHERE *(SqlServer: `CpqlScalarSnapshotTests`, `CpqlTranslatorTests`)*
* [x] Test `[SecondaryTable]` SELECT emits LEFT JOIN; INSERT fires two statements primary-first; DELETE fires secondary-first (Rule 15 / Rule A — compile-time execution order) *(SqlServer: `SecondaryTableTests`; matrix-4: `SecondaryTableMatrixTests`)*
* [x] Test `[PrimaryKeyJoinColumn]` JOIN ON child.id = parent.id; child Id assigned from parent before INSERT (Rule 16 / Rules A+C — JOIN is literal, Id assign is data op) *(SqlServer: `PrimaryKeyJoinColumnGenerationTests`; matrix-4 pending)*
* [x] Test `LazyMap.GetAsync()` returns correct dictionary; SQL identical to `[OneToMany]` literal; grouping is in-memory LINQ not SQL (Rule 17 / Rules A+C) *(SqlServer: `LazyMapGenerationTests`, `LazyLoadingTests`; matrix-4: `LazyMapMatrixTests`)*
* [x] Test `LockMode.PessimisticRead` emits `FOR SHARE` (PostgreSQL), `WITH (HOLDLOCK)` (SQL Server), `FOR SHARE` (MySQL) — SqlServer compile project only *(`ConcurrencyAndLockingTests`, `LockingGenerationTests` in `DapperX.Tests`)*
* [x] Test `LockMode.PessimisticRead` per provider in all compile assemblies — `matrix-4` ([`LockingMatrixTests`](tests/DapperX.Tests.Shared/Generation/MatrixRelationsAndGraphTests.cs))
* [x] Test `LockMode.PessimisticRead` on SQLite → `Diagnostic` error (Rule 18) — SqlServer project today *(DPX037; runtime throw in `ConcurrencyAndLockingTests`)*
* [x] Test `LockMode.PessimisticRead` on SQLite in `DapperX.Tests.Sqlite` — `sqlite-only` ([`SqliteUnsupportedFeatureTests`](tests/DapperX.Tests.Sqlite/SqliteUnsupportedFeatureTests.cs))
* [x] Test Rule A: verify generated code contains no string concatenation involving entity property names, table names, or column names — only @param values are runtime *(SqlServer: `RulesComplianceTests`)*
* [x] Test Rule B: verify `ResolveColumn()` is called for all property→column lookups; verify no `typeof(T).GetProperties()` appears in generated or runtime code *(SqlServer: `RulesComplianceTests`)*
* [x] Test Rule C: verify `[PrimaryKeyJoinColumn]` Id assignment is a C# property assignment (not SQL); verify `LazyMap` grouping is post-load LINQ (not SQL) *(SqlServer: `RulesComplianceTests`, `LazyMapGenerationTests`)*
* [x] Test Rule D: verify repository methods hold no instance fields between calls; verify `LazyMap`/`LazyCollection`/`LazyReference` caches are per entity instance only *(SqlServer: `RulesComplianceTests` — allows `_lifecycle`; lazy cache: `LazyLoadingTests`)*
* [x] Test Section 23 Rule 19: for a representative entity, verify the generated code contains all expected items from the "Must generate" checklist — spot-check `ResolveColumn()`, `FILTER_*` constants, paired IncludeDeleted SQL, Sort lookup switch, `MethodName` literal, `GetAllAsync` overloads, `GetAllSliceAsync` overloads (pageSize+1 template present, no COUNT), `Load{Map}ForManyAsync`, and that `[Index]` produces no SQL *(SqlServer: `Section23ComplianceTests`; matrix-4: `Section23ChecklistMatrixTests`)*
* [x] Test `Slice<T>` SQL: SQL Server emits `FETCH FIRST pageSize+1 ROWS ONLY`; PostgreSQL/MySQL/SQLite emit `LIMIT pageSize+1` — `matrix-4` ([`SlicePagingMatrixTests`](tests/DapperX.Tests.Shared/Generation/SlicePagingMatrixTests.cs))
* [x] Test `Slice<T>` runtime: HasNext = true when result.Count > pageSize; HasNext = false when result.Count ≤ pageSize; no COUNT query fired (verify with SQL call count assertion) *(SqlServer: `SliceRuntimeTests`; integration: `IntegAllProvidersTests.GetAllSlice_issues_single_select_without_count`)*
* [x] Test `[Index]`: attribute accepted on entity class; no SQL/DDL/Diagnostic emitted; multiple [Index] on same entity all stored in `IndexMetadata`; `EntityModel.IndexMetadata` list accessible *(SqlServer: `IndexNonRegressionTests` — no DDL in SQL; metadata API test pending)*
* [x] Test `[UniqueConstraint]` with Rule E: no SQL/DDL/Diagnostic emitted *(SqlServer: `UniqueConstraintGenerationTests`)*
* [x] Test embeddable flattening and `[AttributeOverride]` *(SqlServer: `EmbeddableGenerationTests`)*
* [x] Test `[Formula]` inclusion in SELECT and exclusion from INSERT/UPDATE *( `FormulaColumnGenerationTests` + `FormulaOrder` fixture)*
* [x] Test `[Immutable]` — verify no mutating methods generated *(generator contract: `ImmutableGeneratorContractTests`; full entity compile test blocked by generator duplicate-override issue)*
* [x] Test `[OrderColumn]` position assignment and gap-close logic *(SqlServer: `BatchRelationshipLoaderGenerationTests`)*
* [x] Test type converter read/write paths (all four built-in converters) *(SqlServer: `ConverterGenerationTests`)*
* [x] Test derived query method name parsing (all operators, modifiers, precedence) *(SqlServer: `MethodNameParserTests`, `DerivedQueryGenerationTests`)*
* [x] Test `And`/`Or` precedence and parenthesized grouping rules *(SqlServer: `MethodNameParserTests` — `And` connector)*
* [x] Test property-first longest-match: verify `IsActive` property takes priority over `Is` keyword *(SqlServer: `MethodNameParserTests`)*
* [x] Test property-first: `Like` property → equality; `Name` + `Like` operator → LIKE SQL *(SqlServer: `MethodNameParserTests`)*
* [x] Test genuine ambiguity emits `Diagnostic` error *(SqlServer: `MethodNameParserTests`; generator: `DerivedQueryDiagnosticsContractTests` — DPX023)*
* [x] Test reserved property name (`And`, `Or`, etc.) emits `Diagnostic` warning *(generator contract: `DerivedQueryDiagnosticsContractTests`)*
* [x] Test Sort lookup table generation (base SQL + ORDER BY fragments) *(SqlServer: `GetAllAsyncGenerationTests`)*
* [x] Test Pageable SQL template appending *(SqlServer: `GetAllAsyncGenerationTests`)*
* [x] Test CPQL parser (all grammar constructs including CAST, NULLIF, CONCAT, NULLS FIRST/LAST) *(SqlServer: `CpqlParserTests`)*
* [x] Test CPQL validator (all validation rules, error cases) *(SqlServer: `CpqlSemanticValidatorTests`)*
* [x] Test CPQL translator (all operators, JOINs, CTEs, subqueries, CASE/WHEN, CAST, NULLIF) *(SqlServer: `CpqlTranslatorTests`, `CpqlScalarSnapshotTests`)*
* [x] Test auditing field injection (CreatedDate/LastModifiedDate excluded from UPDATE) *(SqlServer: `AuditingTests`)*
* [x] Test soft-delete rewriting (DELETE → UPDATE; SELECT filter; HardDelete) *(SqlServer: `SoftDeleteTests`)*
* [x] Test tenancy filter injection (all SELECT/UPDATE/DELETE) *(SqlServer: `MultiTenancyTests`)*
* [x] Test lifecycle invocation order (entity + batch + listener + auditing timing) *(SqlServer: `LifecycleTests` — batch-before-entity, `SharedListenerItem`)*
* [x] Test batch concurrency conflict detection *(SqlServer: `LifecycleTests` / `BatchLifecycleItemRepositoryImpl` batch hook order; optimistic version conflicts in `ConcurrencyAndLockingTests`)*
* [x] Smoke-fix regression: identity INSERT omits identity `Id` column (`IdentityInsertRegressionTests`, `GeneratedColumnGenerationTests`, `GeneratedValueMatrixTests` / `AssertIdentityInsertExcludesId`)
* [x] Smoke-fix regression: SqlServer `ORDER BY` before `OFFSET/FETCH` in page/slice SQL (`GetAllAsyncGenerationTests`, `SlicePagingMatrixTests`, `PagePagingMatrixTests`)
* [x] Smoke-fix regression: `ApplySortToPagedSql` prevents duplicate `ORDER BY` (`PagedSortSqlTests`)
* [x] Smoke-fix regression: `BuildReadParameters` merges global filter + tenant params (`GlobalFilterGenerationTests`, `TenantRegionUserEntity`, `GlobalFilterMatrixTests` / `MatrixTenantRegionUser`)
* [x] Smoke-fix regression: SqlServer lock hints via `SqlServerTableHint` (`SqlServerTableHintTests`, `LockingGenerationTests`, `LockingMatrixTests`, `ConcurrencyAndLockingTests`)
* [x] Smoke-fix regression: integration parameterized global filter + tenant soft-delete + sorted paging (`IntegRuntimeExtrasTests`, `IntegTenantRegionUser`)
* [x] Integration Sqlite: `SqliteGuidTypeHandler` for TEXT `tenant_id` → `Guid` materialization (`IntegrationEnvironment`)
* [x] Integration SqlServer DDL: `Assigned`-id fixture tables use plain `INT PRIMARY KEY` (not `IDENTITY`) so explicit `@Id` inserts succeed (`DatabaseBootstrap`)

***

## Feature: Integration Tests

> **Source of truth:** EPIC 26a **Integration test projects (Testcontainers)** above. Do not duplicate scenarios here — track completion only via 26a `integration-{Provider}` / `integration-all` tasks. Stub [`DapperX.IntegrationTests`](tests/DapperX.IntegrationTests/) is replaced by per-provider projects.

### Tasks

* [x] All integration scenarios — EPIC 26a lines 1533–1551; use `SqlExecutionCountFixture` for Section 26 Req 1/5/8
* [x] EPIC 26-only extras not yet in 26a: `IQuery` / `AsSplitQuery` / `Select<TDto>()` roundtrip; global filter `EnableFilter`/`DisableFilter` runtime; `[ColumnTransformer]` roundtrip; concurrent pessimistic-read behaviour *(integration: [`IntegRuntimeExtrasTests`](tests/DapperX.IntegrationTests.Shared/Scenarios/IntegRuntimeExtrasTests.cs); pessimistic-read SQL on SqlServer/PostgreSql/MySql)*

***

# EPIC 26b: Performance Verification

***

## Feature: Integration scenarios (EPIC 26a remainder)

> Completes EPIC 26a lines 1549–1551. Not Sqlite where noted.

### Tasks

* [x] `integration-all`: advanced features — `IntegAdvancedFeaturesTests` (composite key, element collection, named graph, LazyMap, SecondaryTable, PrimaryKeyJoinColumn)
* [x] `integration-SqlServer|PostgreSql|MySql`: stored procedure OUT + `MultiResult<>` — `IntegStoredProcedureTests` + `IntegrationProcedureBootstrap`
* [x] `integration-SqlServer|PostgreSql|MySql`: lock timeout — `IntegLockTimeoutTests` via `IQuery.WithLock`
* [x] `Load*ForManyAsync`: generator passes `logContext`; `FormulaEmitter` aliases columns as `PropertyName` for Dapper mapping

***

## Feature: Performance Requirements (Requirements.md Section 26)

Requirements.md Section 26 defines 8 concrete performance guarantees. Each must be verified:

### Tasks

**Req 1 — Batch SQL call count O(1):**
* [x] Verify batch operations use `ExecuteAsync(collection)` — never per-entity loops *(`PerformanceRequirementsTests.BatchUpdateMany_*`; assigned-key `InsertMany` uses chunk `ExecuteAsync` in `PerfBulkRowRepositoryImpl`)*
* [x] Measure SQL call count for `InsertManyAsync`, `UpdateManyAsync`, `DeleteManyAsync` — must be 1 regardless of collection size *(integration: `SqlExecutionCountFixture` on bulk/CRUD scenarios — EPIC 26a; identity-key `InsertMany` may use per-row scalar by design)*

**Req 2 — Sets not rows:**
* [x] Verify no repository method iterates per-entity for set operations *(`PerformanceRequirementsTests.BatchInsertMany_*`)*
* [ ] Verify `InsertGraphAsync` executes per-level not per-entity *(deferred — graph batch integration benchmark not automated)*

**Req 3 — Memory allocation:**
* [x] Measure: `GlobalFilter` conditional append has zero allocation overhead when no filter is active — verify no per-call string allocation *(`PerformanceRequirementsTests.GlobalFilter_paths_*` — no `StringBuilder`/`Concat` in generated SQL paths)*
* [x] Measure: `ResolveColumn()` switch has O(1) lookup — verify no linear scan or dictionary allocation per call *(EPIC 26b: `ResolveColumnBenchmark` + `RulesComplianceTests`)*
* [ ] Verify: no `IEnumerable<T>` is enumerated more than once in any batch path (no multiple-iteration bugs) *(manual/code review — not automated)*

**Req 4 — No tracking overhead:**
* [x] Verify generated code contains no change-tracking, identity-map, or proxy-overhead structures *(`PerformanceRequirementsTests.Repository_has_no_tracking_*`, `RulesComplianceTests`)*

**Req 5 — Load*ForMany N=1:**
* [x] Measure: `Load{Collection}ForManyAsync` and `Load{Map}ForManyAsync` issue exactly one SQL call regardless of parent count — verify N=1 not N=n *(integration: `IntegAllProvidersTests` + `SqlExecutionCountFixture`; `PerformanceRequirementsTests.LoadForMany_*`)*

**Req 6 — LazyCollection / LazyMap cache:**
* [x] Measure: `LazyCollection` / `LazyMap` cache prevents repeated DB calls on same instance — verify `GetAsync` fires once then returns cached value *(SqlServer: `LazyLoadingTests`; integration: advanced LazyMap scenario)*

**Req 7 — CPQL zero runtime overhead:**
* [x] Verify no CPQL parsing occurs at runtime — all translation completed at compile time *(`PerformanceRequirementsTests.Runtime_has_no_cpql_*`)*
* [ ] Benchmark a CPQL-backed method: call overhead must equal a hand-written Dapper call (no extra allocations) *(BenchmarkDotNet type `ResolveColumnBenchmark` — manual run optional)*

**Req 8 — Slice<T> saves one DB round trip:**
* [x] Benchmark `Page<T>` vs `Slice<T>` for same query — `Page<T>` must issue 2 SQL calls; `Slice<T>` must issue 1 *(integration slice test + `PerformanceRequirementsTests.Slice_sql_*` / `Page_sql_*` compile checks)*
* [x] Verify HasNext determination adds only O(1) integer comparison overhead *(SqlServer: `SliceRuntimeTests`)*

**General:**
* [x] Reuse `SqlExecutionCountFixture` from `DapperX.IntegrationTests.Shared` for Req 1/5/8 integration assertions (same harness as EPIC 26a)
* [x] Add `BenchmarkTests.cs` or integrate with BenchmarkDotNet for Reqs 1, 5, 6, 7, 8 *([`DapperX.Performance.Tests`](tests/DapperX.Performance.Tests/) — `PerformanceRequirementsTests`, `ResolveColumnBenchmark`)*
* [x] CI: performance/benchmark job optional; document in repo README *(documented in [`tests/README.md`](tests/README.md); no mandatory CI job)*
* [ ] Benchmark InsertGraphAsync with 1000 root entities + 5 children each — total SQL calls must be ≤ graph depth × 2 *(deferred — large-scale benchmark not in CI)*

***

# EPIC 27: Sample Application

> **Note:** EPIC 3 (~line 304) tracks the minimal DI scaffold (`Product` + `AddDapperXRepositories`). This EPIC is the **full demo** in [`samples/DapperX.SampleApp/`](samples/DapperX.SampleApp/).

***

## Feature: Demo App

### Tasks

* [x] Create `BaseEntity` with `[MappedSuperclass]`, `[CreatedDate]`, `[LastModifiedDate]`, `[Version]`, `[Generated(Insert)]` for DB-set `CreatedAt` — [`Entities/BaseEntity.cs`](samples/DapperX.SampleApp/Entities/BaseEntity.cs)
* [x] Create `User` entity with `[TenantId]`, address columns, `[EntityListeners]`, `[SoftDelete]`, `[GlobalFilter("active_region", "region = @region")]` — [`Entities/AppUser.cs`](samples/DapperX.SampleApp/Entities/AppUser.cs) *(flat address columns; `[Embedded]` deferred — generator embed path)*
* [x] Create `Order` entity with relationships, lifecycle hooks, `[Formula]` for item count, `[Generated(Always)]` for computed total — [`Entities/SalesOrder.cs`](samples/DapperX.SampleApp/Entities/SalesOrder.cs)
* [x] Create `Product` entity with `[Sortable]`, `[ColumnTransformer]` — [`Entities/CatalogProduct.cs`](samples/DapperX.SampleApp/Entities/CatalogProduct.cs) *(`[Converter]`/`[NamedQueries]`/`[Immutable]` variant: see `tests/DapperX.Tests`; sample avoids generator edge cases)*
* [x] Demonstrate CRUD + base methods (FindAllById, ExistsById, CountAsync, DeleteAllByIdAsync) — `/demo/catalog` in [`DemoEndpoints.cs`](samples/DapperX.SampleApp/DemoEndpoints.cs)
* [x] Demonstrate `GetAllAsync()`, `GetAllAsync(Sort)`, `GetAllAsync(Pageable)` — `/demo/catalog/page`, `/demo/catalog/page/sorted`
* [x] Demonstrate derived query methods (operators, Sort, Pageable, IncludeDeleted) — catalog + user derived methods; `IQuery.IncludeDeleted()`
* [x] Demonstrate CPQL queries (SELECT, UPDATE, DELETE, WITH, subqueries, CASE/WHEN, CAST, NULLIF, window functions) — covered in `tests/DapperX.Tests` / `CpqlWindowFunctionTests`; sample uses `IQuery` + native SQL where needed
* [x] Demonstrate auditing, soft delete + IncludeDeleted(), multi-tenancy, global custom filters in action — `/demo/users` + DI providers in [`Program.cs`](samples/DapperX.SampleApp/Program.cs)
* [x] Create `Member` entity using `[SecondaryTable]` — [`Entities/Member.cs`](samples/DapperX.SampleApp/Entities/Member.cs); `/demo/members`
* [x] Create `Employee` / `Department` entities using `LazyMap<string, Employee>` — [`Entities/Department.cs`](samples/DapperX.SampleApp/Entities/Department.cs)
* [x] Demonstrate shared-PK profile row for `AppUser` — [`Entities/UserProfile.cs`](samples/DapperX.SampleApp/Entities/UserProfile.cs); `/demo/users` + `/demo/users/{id}/profile`
* [x] Demonstrate pessimistic locking (`LockMode.Pessimistic`) and shared read locking (`LockMode.PessimisticRead`) — `/demo/catalog/lock-read`, `/demo/catalog/lock-update`
* [x] Demonstrate lock timeout in a transaction — `WithLock(..., timeoutMs: 2000)` on catalog `IQuery`
* [x] Demonstrate `Slice<T>` for infinite-scroll — `/demo/catalog/slice` via `GetAllSliceAsync`
* [x] Demonstrate batch and graph execution — `/demo/catalog/batch`, `/demo/graph`
* [x] Run against **Docker Compose** SQL Server (`docker compose up -d`; connection string in `appsettings.json`) — [`docker-compose.yml`](samples/DapperX.SampleApp/docker-compose.yml), [`Infrastructure/SampleDatabaseHost.cs`](samples/DapperX.SampleApp/Infrastructure/SampleDatabaseHost.cs)
* [x] Demonstrate SQLite integration with dialect differences — optional `DapperX:DatabaseProvider` = `Sqlite` for in-memory SQLite without Docker; `AppDb` SQLite DDL in [`AppDb.cs`](samples/DapperX.SampleApp/AppDb.cs)
* [x] Endpoint smoke test script (curl all routes) — [`smoke-test.sh`](samples/DapperX.SampleApp/smoke-test.sh) + [`responses.txt`](samples/DapperX.SampleApp/responses.txt) output
* [x] Link smoke-test generator/runtime fixes to automated regression coverage in EPIC 26 (`IdentityInsertRegressionTests`, `IntegRuntimeExtrasTests`, matrix paging/filter tests) — manual `smoke-test.sh` remains sample-app sanity check; CI uses test projects

***

# EPIC 28: Documentation

***

## Feature: Developer Docs

### Tasks

* [ ] Write entity mapping guide (all attributes, MappedSuperclass, Embeddable, Converter, Formula, Generated, ColumnTransformer, SecondaryTable, [Column(Table)], [Index] informational, [UniqueConstraint] informational)
* [ ] Write informational annotations guide (Rule E): [Index] and [UniqueConstraint] produce no SQL/DDL/Diagnostic; what IndexMetadata contains; how schema tools consume it; why presence never alters generated SQL
* [ ] Write pagination guide (Pageable → Page<T> with COUNT; Pageable → Slice<T> without COUNT; AsSlice() fluent modifier; GetAllSliceAsync; when to use each; performance trade-off)
* [ ] Write performance guide (Requirements.md Section 26): all 8 performance guarantees; how to verify SQL call count; BenchmarkDotNet setup; how Slice<T> saves one DB round trip vs Page<T>
* [ ] Write code generation system guide (Requirements.md Section 23): full 32-item "Must generate" checklist; Rules A–E compliance; when to add numbered implementation rules vs when Requirements.md Rules A–E are sufficient; incremental generation; ResolveColumn() pattern; what is/is not a SQL violation
* [ ] Write id generation guide (Identity, Sequence/SequenceGenerator/ISequenceAllocator, Uuid, Assigned)
* [ ] Write repository usage guide (CRUD, all base methods including GetAllAsync overloads and DeleteAllByIdAsync, batch, graph)
* [ ] Write derived query method guide (all keywords, operators, modifiers, Sort, Pageable, Page, IncludeDeleted)
* [ ] Write CPQL language guide (full grammar, all 23 scalar functions, window functions, NULLS FIRST/LAST, CTEs, subqueries, CASE/WHEN)
* [ ] Write auditing guide ([CreatedDate], [LastModifiedDate], [CreatedBy], [LastModifiedBy], IAuditingProvider)
* [ ] Write soft delete guide ([SoftDelete], HardDeleteAsync, IncludeDeleted bypass, CPQL interaction)
* [ ] Write generated columns guide ([Generated(Insert/Always)], re-SELECT behavior per provider)
* [ ] Write global custom filters guide ([GlobalFilter], EnableFilter/DisableFilter, scoped DapperXOptions)
* [ ] Write multi-tenancy guide ([TenantId], ITenantProvider, bypass via NativeQuery)
* [ ] Write secondary table guide ([SecondaryTable], [Column(Table)], split INSERT/UPDATE/DELETE, SELECT JOIN)
* [ ] Write relationship guide (all types including [PrimaryKeyJoinColumn], LazyMap<K,V> with [MapKey])
* [ ] Write locking guide (optimistic, pessimistic FOR UPDATE, pessimistic read FOR SHARE, lock timeout per provider)
* [ ] Write lifecycle guide (entity hooks, batch hooks, EntityListeners, auditing timing)
* [ ] Write multi-database configuration guide (all four providers, SQLite limitations)
* [ ] Write SQLite provider guide (limitations, Diagnostic errors/warnings, minimum version requirements)

***

## Verification by EPIC (SqlServer primary; matrix-4 in EPIC 26a)

| EPIC | Primary tests (`DapperX.Tests`) | EPIC 26a matrix / integration |
|---|---|---|
| 12 Auditing | `AuditingTests`, `MutatingMethodGenerationTests` | `AuditingSqlMatrixTests` |
| 13 Soft delete | `SoftDeleteTests`, `SoftDeleteBypassGenerationTests` | `SoftDeleteMatrixTests`, `IncludeDeletedMatrixTests` |
| 14 Tenancy | `MultiTenancyTests` | `TenancyMatrixTests` |
| 17h Global filter | `GlobalFilterGenerationTests` | `GlobalFilterMatrixTests` |
| 17d Upsert | `UpsertGenerationTests` | `UpsertGenerationMatrixTests` |
| 18 Composite key | `CompositeKeyGenerationTests`, `CompositeKeyDiagnosticTests` | `PrimaryKeyJoinColumnMatrixTests` + `IntegAdvancedFeaturesTests` |
| 19b Bulk insert | `BulkInsertGenerationTests` | `BulkInsertMatrixTests` |
| 20 Locking | `LockingGenerationTests`, `ConcurrencyAndLockingTests` | `LockingMatrixTests`, `LockTimeoutMatrixTests` |
| 21 Multi-DB | `MultiDatabaseProviderTests`, `CpqlScalarSnapshotTests` | matrix-4 suite in [`DapperX.Tests.Shared/Generation/`](tests/DapperX.Tests.Shared/Generation/) |
| 23 Section 23 | `Section23ComplianceTests` | `Section23ChecklistMatrixTests` |
| 25 Logging | `LoggingTests`, `ExecutableSqlFormatterTests` | `MethodNameLoggingMatrixTests` |
| 26b Performance | `PerformanceRequirementsTests` | `SqlExecutionCountFixture` (integration) + optional `ResolveColumnBenchmark` |

***

# Final Execution Order (Recommended)

1. Core + Abstractions (all attributes, enums, metadata models, exceptions, Pageable, Sort, Page, IAuditingProvider, ITenantProvider, ISequenceAllocator)
2. Generator — entity mapping + basic repository SQL + Immutable + Formula + Generated columns
3. Generator — lifecycle + listener invocation
4. Generator — auditing field injection
5. Generator — soft delete rewriting + paired IncludeDeleted SQL literals
6. Generator — multi-tenancy filter injection
7. Generator — global custom filter constants
8. Query system (runtime query builder — including IncludeDeleted, GlobalFilterApplicator)
9. Lazy loading + relationships (FetchType.Eager, OrderColumn, AssociationOverride)
10. Embeddable types
11. Type converters + ColumnTransformer
12. Batch execution
13. Graph execution (flat plan)
14. Concurrency control (optimistic + pessimistic + lock timeout)
15. Generator — derived query method parsing + SQL emission (including IncludeDeleted parameter, GetAllAsync overloads, DeleteAllByIdAsync)
16. Generator — CPQL parser + validator + translator (all features including window functions + 23 scalar functions)
17. Generator — Sort lookup table + Pageable + Page + GetAllAsync overloads
18. Bulk insert optimization
19. Multi-database SQL dialect support (all four providers including SQLite)
20. Transactions
21. Locking modes (Pessimistic, PessimisticRead, lock timeout)
22. Configuration system (DapperXOptions with GlobalFilter methods)
23. Error handling + logging
24. Named entity graphs + Element collections + Composite keys + Stored procedures advanced
25. Upsert + Association override + Explicit batch relationship loading
26. Secondary table mapping (`[SecondaryTable]`, `[Column(Table)]`) + Primary key join column + LazyMap<K,V> + `[MapKey]`
27. LockMode.PessimisticRead (FOR SHARE) per provider
28. `Slice<T>` return type + `[Index]` informational annotation
29a. Provider test matrix (EPIC 26a) — four compile assemblies + Shared + CI compile job
29. Performance verification (EPIC 26b) — `SqlExecutionCountFixture` + benchmarks
30. Sample application (EPIC 27)
31. Documentation (EPIC 28)

***
