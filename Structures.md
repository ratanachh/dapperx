# DapperX – Full Project Structure

***

# 1. Solution Structure

```
DapperX.slnx
│
├── Requirements.md, Tasks.md, Structures.md, CLAUDE.md
├── .github/workflows/ci.yml            -- compile-tests + integration-tests jobs
│
├── src/
│   ├── DapperX.Core/
│   ├── DapperX.Abstractions/
│   ├── DapperX.Runtime/
│   ├── DapperX.Query/
│   ├── DapperX.Relations/
│   ├── DapperX.Lifecycle/
│   ├── DapperX.Batching/
│   ├── DapperX.Provider/          -- SQL Server, PostgreSQL, MySQL, SQLite
│   ├── DapperX.Generator/
│
├── samples/
│   ├── DapperX.SampleApp/
│
├── tests/
│   ├── README.md
│   ├── DapperX.Tests.ProviderTestProject.props
│   ├── DapperX.IntegrationTests.ProviderTestProject.props
│   ├── DapperX.Tests/                    -- SqlServer compile target (DapperXDatabaseProvider=SqlServer)
│   ├── DapperX.Tests.PostgreSql/
│   ├── DapperX.Tests.MySql/
│   ├── DapperX.Tests.Sqlite/
│   ├── DapperX.Tests.Shared/             -- matrix-4 linked sources (no test runner csproj)
│   ├── DapperX.IntegrationTests.Shared/
│   ├── DapperX.IntegrationTests.SqlServer/
│   ├── DapperX.IntegrationTests.PostgreSql/
│   ├── DapperX.IntegrationTests.MySql/
│   ├── DapperX.IntegrationTests.Sqlite/
│   └── DapperX.IntegrationTests/         -- deprecated stub folder (not in slnx)
│
└── docs/
```

***

# 2. Project Responsibilities

***

## 2.1 DapperX.Core

> Core types: attributes, enums, metadata models. No dependencies on other DapperX projects.

```
DapperX.Core/
│
├── Attributes/
│   │
│   ├── Entity/
│   │   ├── EntityAttribute.cs
│   │   ├── MappedSuperclassAttribute.cs
│   │   ├── EmbeddableAttribute.cs
│   │   ├── ImmutableAttribute.cs
│   │
│   ├── Mapping/
│   │   ├── TableAttribute.cs           -- name, Schema, UniqueConstraint(s)
│   │   ├── SecondaryTableAttribute.cs  -- maps entity columns to a second table sharing the same PK
│   │   ├── UniqueConstraintAttribute.cs -- multi-column unique constraint hint (informational only; no SQL/DDL emitted)
│   │   ├── IndexAttribute.cs           -- index documentation hint (informational, not DDL; Columns, Name, Unique)
│   │   ├── ColumnAttribute.cs          -- Nullable, Insertable, Updatable, Unique, Length, Precision, Scale, ColumnDefinition, Fetch, Table (secondary table routing)
│   │   ├── IdAttribute.cs
│   │   ├── GeneratedValueAttribute.cs
│   │   ├── SequenceGeneratorAttribute.cs -- Name, SequenceName only (no AllocationSize)
│   │   ├── VersionAttribute.cs
│   │   ├── TransientAttribute.cs
│   │   ├── SortableAttribute.cs
│   │   ├── FormulaAttribute.cs
│   │   ├── EmbeddedAttribute.cs
│   │   ├── AttributeOverrideAttribute.cs
│   │   ├── ConverterAttribute.cs
│   │   ├── EnumeratedAttribute.cs      -- shorthand: EnumType.String / EnumType.Ordinal
│   │   ├── ColumnTransformerAttribute.cs -- Read / Write SQL expression (SQL-level transform)
│   │   ├── ProjectionAttribute.cs
│   │
│   ├── Relations/
│   │   ├── OneToManyAttribute.cs
│   │   ├── ManyToOneAttribute.cs
│   │   ├── OneToOneAttribute.cs
│   │   ├── ManyToManyAttribute.cs
│   │   ├── JoinColumnAttribute.cs
│   │   ├── JoinTableAttribute.cs
│   │   ├── OrderByAttribute.cs
│   │   ├── OrderColumnAttribute.cs
│   │   ├── AssociationOverrideAttribute.cs -- overrides FK column for inherited/embedded relationships
│   │   ├── PrimaryKeyJoinColumnAttribute.cs -- shared-PK OneToOne; child.Id = parent.Id, no separate FK
│   │   ├── MapKeyAttribute.cs               -- specifies key column for LazyMap<K,V> relationships
│   │
│   ├── Lifecycle/
│   │   ├── PrePersistAttribute.cs
│   │   ├── PostPersistAttribute.cs
│   │   ├── PreUpdateAttribute.cs
│   │   ├── PostUpdateAttribute.cs
│   │   ├── PreRemoveAttribute.cs
│   │   ├── PostRemoveAttribute.cs
│   │   ├── PostLoadAttribute.cs
│   │   ├── PrePersistBatchAttribute.cs
│   │   ├── PostPersistBatchAttribute.cs
│   │   ├── PreUpdateBatchAttribute.cs
│   │   ├── PostUpdateBatchAttribute.cs
│   │   ├── PreRemoveBatchAttribute.cs
│   │   ├── PostRemoveBatchAttribute.cs
│   │   ├── EntityListenersAttribute.cs
│   │
│   ├── Auditing/
│   │   ├── CreatedDateAttribute.cs
│   │   ├── LastModifiedDateAttribute.cs
│   │   ├── CreatedByAttribute.cs
│   │   ├── LastModifiedByAttribute.cs
│   │
│   ├── Behavior/
│   │   ├── SoftDeleteAttribute.cs
│   │   ├── TenantIdAttribute.cs
│   │   ├── GlobalFilterAttribute.cs   -- Name, Condition (native SQL fragment with @param)
│   │   ├── GeneratedAttribute.cs      -- GenerationTime.Insert / GenerationTime.Always
│   │
│   ├── CompositeKey/
│   │   ├── IdClassAttribute.cs
│   │   ├── EmbeddedIdAttribute.cs
│   │
│   ├── ElementCollection/
│   │   ├── ElementCollectionAttribute.cs
│   │   ├── CollectionTableAttribute.cs
│   │
│   ├── EntityGraph/
│   │   ├── NamedEntityGraphAttribute.cs
│   │   ├── SubGraphAttribute.cs
│   │
│   ├── Query/
│   │   ├── RepositoryAttribute.cs      -- [Repository] (no args); marks interface for generation; entity type inferred from IRepository<TEntity,TId>; emits {Name}RepositoryImpl sealed class
│   │   ├── QueryAttribute.cs
│   │   ├── NamedQueryAttribute.cs
│   │   ├── NamedQueriesAttribute.cs
│   │   ├── StoredProcedureAttribute.cs
│   │   ├── BulkOperationAttribute.cs
│
├── Configuration/
│   ├── DapperXDatabaseProviderAttribute.cs  -- assembly-level compile-time provider (SqlServer default; MSBuild DapperXDatabaseProvider overrides)
│
├── Enums/
│   ├── CascadeType.cs
│   ├── DatabaseProvider.cs         -- SqlServer, PostgreSql, MySql, SQLite
│   ├── GenerationType.cs
│   ├── FetchType.cs
│   ├── LockMode.cs                  -- Optimistic, Pessimistic (FOR UPDATE), PessimisticRead (FOR SHARE)
│   ├── CpqlType.cs
│   ├── EnumType.cs                  -- String, Ordinal (for [Enumerated] shorthand)
│   ├── GenerationTime.cs            -- Insert, Always (for [Generated] annotation)
│
├── Models/
│   ├── EntityMetadata.cs
│   ├── PropertyMetadata.cs           -- includes column name, nullable, insertable, updatable, fetch, secondaryTableName, formula, converter, sortable, auditing flags
│   ├── RelationshipMetadata.cs       -- includes FetchType, JoinColumn, JoinTable, OrderColumn, isPrimaryKeyJoin flag, mapKeyColumn
│   ├── EmbeddedMetadata.cs
│   ├── ConverterMetadata.cs
│   ├── FormulaMetadata.cs
│   ├── SequenceMetadata.cs          -- holds SequenceName only; no AllocationSize (stateless rule)
│   ├── NamedQueryMetadata.cs
│   ├── AuditingMetadata.cs
│   ├── SoftDeleteMetadata.cs
│   ├── TenancyMetadata.cs
│   ├── CompositeKeyMetadata.cs
│   ├── ElementCollectionMetadata.cs
│   ├── NamedEntityGraphMetadata.cs
│   ├── ColumnTransformerMetadata.cs  -- Read/Write SQL expressions per property
│   ├── AssociationOverrideMetadata.cs
│   ├── GeneratedMetadata.cs          -- GenerationTime + re-SELECT SQL per property
│   ├── GlobalFilterMetadata.cs       -- Name + Condition fragment per entity
│   ├── SecondaryTableMetadata.cs     -- secondary table name + PK join column + property list
│   ├── MapKeyMetadata.cs             -- key column name for LazyMap<K,V> relationships
│   ├── IndexMetadata.cs              -- informational: column list, name, unique flag; no SQL/DDL emitted
```

***

## 2.2 DapperX.Abstractions

> Interfaces, contracts, exceptions, and shared value types. No dependencies on other DapperX projects.

```
DapperX.Abstractions/
│
├── Repositories/
│   ├── IRepository.cs              -- CRUD reads with optional `bool includeDeleted` (soft-delete bypass); GetAllSliceAsync → Slice<T>
│
├── Query/
│   ├── IQuery.cs
│
├── Lifecycle/
│   ├── ILifecycleInvoker.cs
│
├── Converters/
│   ├── IValueConverter.cs
│
├── Paging/
│   ├── Pageable.cs
│   ├── Page.cs
│   ├── Slice.cs                    -- content + HasNext flag; no COUNT query; more efficient for infinite-scroll
│
├── Sorting/
│   ├── Sort.cs
│
├── Auditing/
│   ├── IAuditingProvider.cs
│
├── Tenancy/
│   ├── ITenantProvider.cs
│
├── Sequences/
│   ├── ISequenceAllocator.cs       -- optional injection; developer implements; DapperX emits NextAsync("seq") call
│
├── Logging/
│   ├── DapperXLogEntry.cs          -- contract type: Sql, Parameters, ExecutableSql, MethodName, Timestamp
│
├── StoredProcedures/
│   ├── ProcParam.cs                -- parameter definition (Name, ParameterMode, Type)
│   ├── ProcResult.cs               -- return type for OUT params (ProcResult<T1>, ProcResult<T1,T2>)
│   ├── MultiResult.cs              -- return type for multiple result sets (MultiResult<T1,T2>)
│   ├── ParameterMode.cs            -- enum: In, Out, InOut, Return
│
├── Graphs/
│   ├── InvalidEntityGraphException.cs
│
├── Exceptions/
│   ├── ConcurrencyException.cs
│   ├── MappingException.cs
│   ├── SqlExecutionException.cs
│   ├── InvalidSortException.cs
│   ├── UnmappedPropertyException.cs  -- thrown by ResolveColumn() for unrecognised property name
│
├── Configuration/
│   ├── IDapperXOptions.cs          -- includes LogSql, LogParameters, LogExecutableSql, Logger; EnableFilter/DisableFilter/IsFilterActive for global filters
```

***

## 2.3 DapperX.Query

> Runtime query builder and expression translation. Depends on Core and Abstractions.

```
DapperX.Query/
│
├── Query/
│   ├── Query.cs
│   ├── QueryBuilder.cs             -- fluent API: Where, OrderBy, Skip/Take, Include, WithLock, IncludeDeleted, AsSlice, AsSplitQuery, Select<TDto>
│
├── Expressions/
│   ├── ExpressionParser.cs
│   ├── WhereTranslator.cs
│   ├── OrderByTranslator.cs
│
├── Filters/
│   ├── SoftDeleteBypassSelector.cs -- selects between with-filter / without-filter SQL literals based on IncludeDeleted flag
│
├── Projections/
│   ├── ProjectionMaterializer.cs   -- `EnsureProjection<TDto>()` validates `[Projection]`; Dapper maps column aliases to DTO properties
│
├── Sql/
│   ├── SqlBuilder.cs
│   ├── SqlParameterBuilder.cs
```

***

## 2.4 DapperX.Relations

> Lazy loading wrappers and relationship loaders. Depends on Core and Abstractions.

```
DapperX.Relations/
│
├── Lazy/
│   ├── LazyCollection.cs           -- read-once + thread-safe first load (SemaphoreSlim); GetAsync, TryGet, Set; no Reload
│   ├── LazyReference.cs            -- GetAsync, TryGet, Set; no Reload (read-once model)
│   ├── LazyMap.cs                  -- read-once + thread-safe first load; GetAsync/TryGet/Set; in-memory grouping by [MapKey]
│
├── Loaders/
│   ├── CollectionLoader.cs
│   ├── ReferenceLoader.cs
│   ├── MapLoader.cs                -- loads LazyMap<K,V>; executes same SELECT as CollectionLoader; groups by [MapKey] column in-memory
│
├── Helpers/
│   ├── RelationshipHelper.cs
```

***

## 2.5 DapperX.Lifecycle

> Lifecycle execution base classes and entity listener wiring. Depends on Core and Abstractions.

```
DapperX.Lifecycle/
│
├── Invokers/
│   ├── LifecycleInvokerBase.cs
│   ├── EntityLifecycleInvoker.cs     -- per-entity generated invoker base; listener calls emitted at compile time
│
├── Batch/
│   ├── BatchLifecycleInvoker.cs
```

***

## 2.6 DapperX.Batching

> Batch execution, graph flattening, topological ordering, and execution plans. Depends on Core and Abstractions.

```
DapperX.Batching/
│
├── Execution/
│   ├── ExecutionPlan.cs
│   ├── ExecutionNode.cs
│   ├── ExecutionEngine.cs            -- compile-time metadata only; graph repos use imperative GraphGenerator loops
│
├── Batch/
│   ├── BatchExecutor.cs
│   ├── BatchChunker.cs              -- validated deterministic chunking (`chunkSize > 0`); preserves source order
│
├── Graph/
│   ├── GraphBuilder.cs
│   ├── DependencyResolver.cs
│   ├── TopologicalSorter.cs
```

***

## 2.7 DapperX.Provider

> Database-specific SQL fragments and bulk executors. Depends on Core and Abstractions.

```
DapperX.Provider/
│
├── Common/
│   ├── IDatabaseProvider.cs
│   ├── IBulkInsertExecutor.cs
│   ├── BulkInsertContext.cs
│   ├── BulkInsertDataTableBuilder.cs
│   ├── DatabaseProviderFactory.cs
│   ├── DatabaseProviderBase.cs
│   ├── SqlDialect.cs
│
├── SqlServer/
│   ├── SqlServerProvider.cs
│   ├── SqlServerBulkExecutor.cs      -- SqlBulkCopy; used when InsertMany count >= BulkThreshold
│   ├── SqlServerDialect.cs
│
├── PostgreSql/
│   ├── PostgreSqlProvider.cs
│   ├── PostgreSqlBulkExecutor.cs     -- Npgsql binary COPY
│   ├── PostgreSqlDialect.cs
│
├── MySql/
│   ├── MySqlProvider.cs
│   ├── MySqlBatchExecutor.cs         -- MySqlBulkCopy
│   ├── MySqlDialect.cs
│
├── Sqlite/
│   ├── SqliteProvider.cs             -- BulkInsertExecutor null; silent batch fallback
│   ├── SqliteDialect.cs
```

`SqlDialect` implementations handle all provider-specific SQL differences: paging syntax (`OFFSET/FETCH` vs `LIMIT/OFFSET`), `Slice<T>` paging syntax (`FETCH FIRST pageSize+1 ROWS ONLY` for SQL Server; `LIMIT pageSize+1` for PostgreSQL / MySQL / SQLite), identity return (`OUTPUT INSERTED.Id` / `RETURNING id` / `LAST_INSERT_ID()` / `last_insert_rowid()`), pessimistic lock hints (SQLite: Diagnostic error), lock timeout (integer `@timeout` ms; MySQL: NOWAIT only; SQLite: Diagnostic error), sequence syntax (SQLite: Diagnostic error), boolean literals, regex operators (SQLite: Diagnostic warning), upsert syntax, stored procedure call syntax (SQLite: Diagnostic error), CTE syntax, window function syntax (all four providers; MySQL 8.0+; SQLite 3.25.0+), cross-provider scalar function mapping — 23 functions including `LENGTH` → `LEN` (SQL Server), `CONCAT` → `||` (SQLite), `YEAR`/`MONTH`/`DAY` → `strftime` (SQLite), `SUBSTRING` → `SUBSTR` (SQLite), `LEFT`/`RIGHT` → `SUBSTR` (SQLite), `MOD` → `%` (SQL Server / SQLite), `CAST` type name mapping, `NULLS FIRST/LAST` emulation, bulk UPDATE `FROM … JOIN` syntax, `CURRENT_TIMESTAMP` / `CURRENT_DATE` dialect form, generated column re-SELECT syntax (`OUTPUT INSERTED` / `RETURNING` / separate SELECT), table schema handling (SQLite: Diagnostic warning — ignored), and soft-delete boolean column syntax.

***

## 2.8 DapperX.Runtime

> Shared runtime utilities, configuration, built-in converters, and the Dapper execution wrapper. Depends on Provider and Batching.

```
DapperX.Runtime/
│
├── Repositories/
│   ├── DapperXRepositoryBase.cs    -- abstract IRepository impl; Options + Provider for logging; LogContext() → DbExecutor
│   │                                  All Dapper call logic lives here (GetByIdAsync, InsertAsync, etc.)
│   │                                  WithTransactionAsync(Func<IDbTransaction, Task>) — Requirements §17 wrapper (BeginTransaction/Commit/Rollback)
│   │                                  SQL strings are abstract properties — overridden by generated {Name}RepositoryImpl
│   │                                  Generator emits code that extends this class; base class never has entity-specific code
│   │                                  virtual Query() throws — generated repos override with RepositoryQuery
│   │                                  generated InsertMany/UpdateMany/DeleteMany use chunked `ExecuteAsync(sql, chunk, tx)` in source order
│
├── Query/
│   ├── QueryRuntimeConfig.cs       -- Provider, SoftDeleteSupported, SoftDeleteColumn, tenant/global-filter delegates, IncludeJoinSql, ProjectionBaseSql, lock suffix/preamble templates
│   ├── QueryBuilderStateSnapshot.cs -- non-generic fluent state carried across entity → DTO projection (Select)
│   ├── QueryExecutor.cs            -- composes base SELECT + runtime WHERE/ORDER BY/paging/lock timeout preamble (Pattern 4)
│   ├── SqlServerTableHint.cs       -- inserts SQL Server table hints after FROM table reference (smoke-fix regression: `SqlServerTableHintTests`)
│   ├── RepositoryQuery.cs          -- IQuery<T> implementation; ToList/First/Page/Slice/AsyncEnumerable
│
├── Configuration/
│   ├── DapperXOptions.cs           -- BatchSize, BulkThreshold, Logger, LogSql/Parameters/ExecutableSql; EnableFilter/DisableFilter/IsFilterActive via thread-safe ConcurrentDictionary — scoped per DI instance
│
├── Execution/
│   ├── DbExecutor.cs               -- wraps Dapper calls; SqlExecutionException with SQL context; invokes SqlExecutionLogger before Dapper when LogSql + Logger set
│   ├── DbExecutionLogContext.cs    -- MethodName, Options, Provider passed from generated repos / RepositoryQuery
│
├── Logging/
│   ├── SqlExecutionLogger.cs       -- builds DapperXLogEntry; TryLogBatchTrace for batch/graph sizing
│   ├── ParameterExtractor.cs       -- anonymous object / DynamicParameters → dictionary for LogParameters
│   ├── ExecutableSqlFormatter.cs   -- substitutes @param values into SQL for human-readable output (dialect-aware; never executed)
│
├── Converters/
│   ├── EnumToStringConverter.cs
│   ├── EnumToIntConverter.cs
│   ├── JsonConverter.cs
│   ├── UtcDateTimeConverter.cs
│
├── Utilities/
│   ├── TypeHelper.cs
│   ├── SqlHelper.cs
```

***

## 2.9 DapperX.Generator

> Roslyn incremental source generator — the core engine. Depends only on Core (via Roslyn SemanticModel, never via runtime assembly loading).

```
DapperX.Generator/
│
├── Generators/
│   ├── RepositoryGenerator.cs          -- emits SQL string property overrides for DapperXRepositoryBase; sealed {Name}RepositoryImpl (not partial, no Dapper call duplication)
│   ├── DerivedQueryGenerator.cs        -- method-name parsing → SQL emitter; maps interface parameters to SQL args; Sort/Pageable/LockMode branches; emitted into {Name}RepositoryImpl
│   ├── ExecutionPlanGenerator.cs       -- compile-time InsertGraphExecutionPlan / DeleteGraphExecutionPlan node lists
│   ├── LifecycleGenerator.cs           -- stub; lifecycle emission lives in LifecycleEmitter
│   ├── QueryGenerator.cs               -- QueryBaseSql/CountFrom + eager joins; QueryProjectionBaseSql; QueryIncludeJoinSql incl. shared-PK OneToOne; split-include attach; Query() factory
│   ├── SortLookupGenerator.cs          -- Sort parameter lookup table emitter
│   ├── CpqlGenerator.cs                -- CPQL string → SQL translator + emitter; Page&lt;T&gt; COUNT pairing; named-query CPQL pass
│   ├── GraphGenerator.cs               -- InsertGraphAsync/UpdateGraphAsync/DeleteGraphAsync; cascade-filtered children; GraphChildRepositoryEmitter DI passthrough; FK assignment; [OrderColumn]; M2M join insert/delete/reconcile on UpdateGraph (Merge); transaction commit/rollback
│   ├── GraphChildRepositoryEmitter.cs  -- emits `new ChildRepositoryImpl(_connection, …)` with parent fields when child+parent share tenant/auditing/options/sequence
│   ├── AuditingGenerator.cs            -- injects GetCurrentUser for By-fields before lifecycle hooks; date fields SQL-only
│   ├── SoftDeleteGenerator.cs          -- shared DELETE→UPDATE SET clause (provider-aware deleted_at); used by SqlBuilder, DerivedQueryGenerator, CpqlTranslator
│   ├── SoftDeleteReadOverrideGenerator.cs -- `includeDeleted` read overrides when entity has [SoftDelete] and no tenancy/global-filter overrides
│   ├── SoftDeleteBypassHelper.cs       -- `*SqlIncludingDeleted` selection expressions shared by tenancy/global-filter/CPQL emitters
│   ├── TenancyGenerator.cs             -- tenant WHERE helpers; WithTenantParams; read/mutation overrides; ApplyTenantIdFromProvider on INSERT
│   ├── NamedEntityGraphGenerator.cs    -- graph FROM/BY-ID SQL + JOINs (AttributeNodes + SubGraphs); `ResolveNamedEntityGraphSql` / `FromSql` switches; `LoadGraphAsync`
│   ├── UpsertGenerator.cs              -- UpsertAsync/UpsertManyAsync overrides (tenant apply; no lifecycle); composite → NotSupported + DPX031
│   ├── BatchRelationshipLoaderGenerator.cs -- Load{Property}ForManyAsync for LazyCollection/LazyMap/ ManyToMany (link table + child SELECT); emits [OrderColumn] position/gap SQL helpers
│   ├── LazyLoaderGenerator.cs          -- WireLazyLoaders on OnPostLoad (relationship loaders + element-collection LazyCollection loaders via Load{Property}Sql)
│   ├── ElementCollectionLifecycleEmitter.cs -- loaded-only Insert/Update/Delete sync for owned element collections on parent persist
│   ├── CompositeKeyGenerator.cs        -- composite GetById/Exists/DeleteById overrides; DPX030 bulk-id restriction; DPX031 upsert warning
│   ├── CompositeKeyMetadataBuilder.cs  -- CompositeKeyModel from [IdClass]/[EmbeddedId]; DPX066/068/069
│   ├── CompositeKeySqlHelper.cs        -- composite WHERE literals; id param objects; TKey type resolution
│   ├── ElementCollectionGenerator.cs   -- collection table SELECT/INSERT/DELETE SQL + Load/Insert/Delete{Property}Async; embeddable flattening + OrderColumn params
│   ├── StoredProcedureGenerator.cs     -- SP call SQL + DynamicParameters (In/Out/InOut); ProcResult OUT capture; MultiResult QueryMultiple
│   ├── ColumnTransformerGenerator.cs   -- injects Read/Write SQL expressions into SELECT/INSERT/UPDATE
│   ├── GeneratedColumnEmitter.cs         -- GeneratedInsertFetchRow + ApplyGeneratedInsertFetch for inline OUTPUT/RETURNING
│   ├── GlobalFilterGenerator.cs        -- FILTER_{NAME} on impl; ApplyGlobalFilters on SELECT/UPDATE/DELETE/CPQL/batch loaders; MappedSuperclass merge in MetadataBuilder
│   ├── MutatingMethodEmitter.cs        -- unified Insert/Update/Delete overrides (audit, tenant, identity, secondary, generated, element collections)
│   ├── MethodNameEmitter.cs            -- MethodName literals on base IRepository methods
│   ├── SecondaryTableGenerator.cs      -- per-table INSERT/UPDATE/DELETE literals (primary-first INSERT, secondary-first DELETE) + LEFT JOIN in SELECT via SqlBuilder
│   ├── SecondaryTableTransactionEmitter.cs -- BeginTransaction/Commit/Rollback when transaction is null on multi-statement secondary paths
│   ├── PrimaryKeyJoinColumnGenerator.cs -- emits ON child.id = parent.id JOIN condition; emits child.Id = parent.Id assignment before child INSERT
│
├── MethodNameParsing/
│   ├── MethodNameParser.cs             -- tokenises method name; strips runtime suffixes (`Sorted`, `Paged`, `WithGraph`, …)
│   ├── PropertyFirstResolver.cs        -- longest-match property vs keyword; operator-prefix ambiguity (Not+Deleted vs NotDeleted)
│   ├── OperatorKeywordTable.cs         -- compile-time table of all reserved operator keywords
│
├── Cpql/
│   ├── CpqlLexer.cs                    -- tokenize keywords, :params, literals, operators
│   ├── CpqlParser.cs                   -- recursive-descent parser → AST
│   ├── CpqlParseResult.cs              -- AST + parse diagnostics
│   ├── CpqlAst.cs                      -- AST nodes (Select/Update/Delete, Join, Where, Case, Subquery, Cte, Window, ScalarFunction, …)
│   ├── CpqlTranslationContext.cs       -- alias table, implicit joins, provider, entity lookup
│   ├── CpqlTranslator.cs               -- AST → provider-specific SQL string literal
│   ├── CpqlScalarFunctions.cs          -- 23-function + CAST/NULLIF/CONCAT emission per provider
│   ├── CpqlToken.cs                    -- lexer token kinds
│
│   (CpqlValidator + CpqlSemanticValidator live under Validation/ — wired before translation)
│
│   Generators/CpqlGenerator.cs         -- `[Query]` CPQL → method body; called from DerivedQueryGenerator
│
├── Models/
│   ├── EntityModel.cs                  -- `Formulas`, `EmbeddedSites`, `RequiresDbRow`, `HasConverters`; element collections, named graphs, lifecycle
│   ├── PropertyModel.cs                -- `IsEmbeddedColumn`, `EmbeddedOwner`/`EmbeddedInner`; `ConverterColumnClrTypeName`
│   ├── DerivedQueryPathModel.cs        -- direct / embedded / navigation FK / navigation JOIN path catalog
│   ├── RelationshipModel.cs
│   ├── EmbeddedModel.cs
│   ├── ConverterModel.cs
│   ├── FormulaModel.cs                 -- property name, verbatim SQL expression, SELECT column alias
│   ├── SequenceModel.cs
│   ├── CteModel.cs
│   ├── DerivedQueryModel.cs            -- parsed method-name query representation
│   ├── AuditingModel.cs                -- CreatedDate/By + LastModifiedDate/By property names; role-driven Insertable/Updatable
│   ├── SoftDeleteModel.cs
│   ├── TenancyModel.cs
│   ├── CompositeKeyModel.cs
│   ├── ElementCollectionModel.cs
│   ├── ExecutionPlanModel.cs           -- ordered ExecutionNode list for graph insert/delete plans
│   ├── ColumnTransformerModel.cs
│   ├── StoredProcedureModel.cs         -- proc name, ProcParam list, ResultSets types, return shape
│   ├── UpsertModel.cs
│   ├── GlobalFilterModel.cs            -- filter name + compile-time SQL fragment constant
│   ├── SecondaryTableModel.cs          -- secondary table name, PK join column, column assignments
│   ├── MapKeyModel.cs                  -- key column name + key type for LazyMap generation
│
├── Builders/
│   ├── MetadataBuilder.cs              -- `ExpandEmbeddedColumns`; `ElementCollection` + `[NamedEntityGraph]` extraction; `[SequenceGenerator]` + `[AssociationOverride]` resolution
│   ├── StoredProcedureMetadataBuilder.cs -- parses `[StoredProcedure]` Out/InOut/Return/ResultSets metadata
│   ├── DerivedQueryPathBuilder.cs      -- builds path catalog + JOIN select core for navigation queries
│   ├── GraphBuilder.cs                 -- graph-capable OneToMany/M2M catalog; cascade filter per Insert/Update/Delete; cycle detection (DPX012)
│   ├── SqlBuilder.cs                   -- compile-time SQL; `applySoftDelete` on read WHERE helpers
│   ├── SoftDeleteSqlBuilder.cs         -- paired active/bypass read SQL for [SoftDelete] entities (RepositoryEmitter)
│   ├── GeneratedColumnSqlBuilder.cs    -- OUTPUT INSERTED / RETURNING / post-INSERT re-SELECT for [Generated]
│   ├── SortFragmentBuilder.cs          -- builds ORDER BY fragment literals per [Sortable] × direction
│   ├── AuditingSqlBuilder.cs           -- provider-aware auditing SQL fragments (GETDATE/NOW/etc.); auditing Insertable/Updatable rules in MetadataBuilder
│   ├── UpsertSqlBuilder.cs             -- MERGE (SqlServer) / ON CONFLICT (PostgreSql, Sqlite) / ON DUPLICATE KEY (MySql) literals
│   ├── RelationshipMetadataEnricher.cs -- second-pass FK/child table resolution for batch loaders
│   ├── RelationshipSqlBuilder.cs       -- compile-time relationship SQL: batch SELECT (+soft-delete/tenant/order), reference JOIN SQL, [OrderColumn] position/gap updates
│   ├── FilterInjector.cs               -- soft-delete/tenancy on WHERE (via `ProviderSqlHelper`); `AppendJoinFilters` on JOIN ON for graph/lazy SQL
│
├── Emitters/
│   ├── RepositoryEmitter.cs
│   ├── RepositoryEmission.cs             -- InsertMany/UpdateMany/DeleteMany; bulk InsertMany when Assigned key + eligible + count >= BulkThreshold
│   ├── BulkInsertGenerator.cs            -- bulk table/column metadata + `BuildBulkInsertRow` for eligible entities
│   ├── ExecutionPlanEmitter.cs
│   ├── LifecycleEmitter.cs             -- `{Entity}LifecycleInvoker` + batch invoker; entity hooks + `[EntityListeners]` direct calls
│   ├── SortLookupEmitter.cs
│   ├── ProjectionEmitter.cs
│   ├── FormulaEmitter.cs               -- verbatim [Formula] SQL in SELECT (`FormatSelectColumn` / `FormatProjectionExpression`); [ColumnTransformer] Read
│   ├── EmbeddedMappingEmitter.cs       -- `{Entity}DbRow`, `MapFromDbRow`, `BuildMutationParameters` for [Embedded]/[Converter]
│   ├── EntityQueryEmitter.cs           -- `QueryAsync`/`QueryFirstOrDefaultAsync` via DbRow; `EmitStandardReadOverrides`
│   ├── ConverterEmitter.cs             -- static converter fields; `ApplyConvertersRead`; mutation `ToColumn`
│   ├── ColumnResolverEmitter.cs         -- emits ResolveColumn(propertyName) per entity; used by WhereTranslator at runtime (no reflection)
│   ├── ColumnTransformerEmitter.cs      -- documents SQL via FormulaEmitter + SqlBuilder (no duplicate logic)
│   ├── UpsertEmitter.cs                 -- (optional; upsert SQL via UpsertSqlBuilder + UpsertSql property in RepositoryEmitter)
│
├── Validation/
│   ├── MappingValidator.cs             -- orchestrates entity mapping validators (key, version, SQLite, PK join, secondary tables, element collections, named graphs, soft-delete, global filters, auditing, composite key, generated columns)
│   ├── CpqlValidator.cs                -- CPQL parameter binding, window placement, nested subquery reject; delegates to CpqlSemanticValidator
│   ├── CpqlSemanticValidator.cs        -- return type vs SELECT, NEW ctor via Compilation, LEFT JOIN nullability, CASE/CAST, IN/EXISTS subqueries, CTE columns (Requirements L1258–1299)
│   ├── RelationshipValidator.cs        -- OneToMany collection type (DPX032); ManyToMany `[JoinTable]` completeness (DPX076–DPX078)
│   ├── ElementCollectionValidator.cs   -- [CollectionTable] required (DPX011)
│   ├── NamedEntityGraphValidator.cs    -- AttributeNodes / SubGraphs match relationships (DPX043, DPX070)
│   ├── StoredProcedureValidator.cs     -- SP parameter names + return-type shape (DPX072–DPX075)
│   ├── SoftDeleteValidator.cs          -- column exists; CPQL bypass scan (DPXCPQL040)
│   ├── GlobalFilterValidator.cs        -- non-empty filter conditions (DPX044); duplicate @param across filters (DPX060 warning)
│   ├── FormulaValidator.cs             -- [Formula] cannot combine with [Id], [Version], or [Sortable] (DPX050–DPX052); not queryable in CPQL (DPX053)
│   ├── EmbeddableValidator.cs          -- [Embeddable] must not have [Id] or [Table] (DPX054–DPX055); [Embedded] type check (DPX056)
│   ├── ConverterValidator.cs           -- [Converter] implements IValueConverter with parameterless ctor (DPX057)
│   ├── AssociationOverrideValidator.cs -- override Name must match relationship on entity/superclass (DPX058)
│   ├── GeneratedColumnValidator.cs     -- invalid GenerationTime → DPX059; [Formula]+[Generated] → DPX009 in MetadataBuilder
│   ├── AuditingValidator.cs            -- CreatedBy/LastModifiedBy warnings (DPX013)
│   ├── CompositeKeyValidator.cs        -- DPX046 Assigned-only; DPX067 embeddable; all key parts validated
│   ├── PropertyNameValidator.cs        -- reserved operator keyword warnings (DPX015)
│   ├── DerivedQueryValidator.cs        -- property paths; write/bulk signatures; regex provider (DPX021–DPX029); IncludeDeleted (DPX022); EntityGraph+Include (DPX071)
│   ├── SecondaryTableValidator.cs      -- DPX061 missing PrimaryKeyJoinColumn; DPX062 duplicate table; DPX063 [Id] on secondary column
│   ├── PrimaryKeyJoinColumnValidator.cs -- [PrimaryKeyJoinColumn] requires child entity Assigned Id (via compilation lookup); conflicts with [JoinColumn] on same property
│   ├── MapKeyValidator.cs              -- DPX034 missing [MapKey]; DPX064 column not on child; DPX065 LazyMap generic type mismatch
│   ├── TenancyValidator.cs             -- [TenantId] without ITenantProvider warning
│
├── DapperXSourceGenerator.cs           -- incremental entry; `MethodSymbolKey` resolves interface overloads by full signature
│
├── Utils/
│   ├── ProviderSqlHelper.cs            -- PostgreSql `= ANY(@param)` IN clauses; boolean literals (`true`/`false` vs `1`/`0`); soft-delete active predicate; threaded through SqlBuilder, RelationshipSqlBuilder, QueryGenerator attach SQL, FilterInjector, StoredProcedureGenerator (PG CALL args)
│   ├── CascadeHelper.cs                -- parse CascadeType flags from relationship attributes; HasPersist/Merge/Remove
│   ├── SyntaxHelper.cs                 -- includes `BulkOperationAttr`; SubGraph attribute parsing
│   ├── BulkInsertEligibility.cs        -- Assigned-key + simple-entity gate for native bulk InsertMany
│   ├── ProjectionCollector.cs          -- scans `[Projection(From=typeof(TEntity))]` DTOs at compile time for QueryGenerator
│   ├── DiagnosticsReporter.cs          -- DPX001–DPX047 (derived query, SQLite, element collection, named graph)
│   ├── CompileTimeDatabaseProvider.cs  -- MSBuild DapperXDatabaseProvider + assembly attribute resolution
```

***

## 2.10 Sample Application

```
samples/DapperX.SampleApp/
│
├── Entities/
│   ├── BaseEntity.cs          -- [MappedSuperclass] auditing, [Version], [Generated(Insert)]
│   ├── AppUser.cs             -- tenancy, soft delete, global filter, entity listeners
│   ├── UserProfile.cs         -- shared PK with AppUser (inserted separately in demo)
│   ├── Member.cs              -- [SecondaryTable]
│   ├── CatalogProduct.cs      -- [Sortable], [ColumnTransformer]
│   ├── SalesOrder.cs          -- formula, generated column, lifecycle, lines
│   ├── Department.cs          -- LazyMap<string, Employee>
│   └── GraphParent.cs         -- graph insert
│
├── Repositories/              -- [Repository] interfaces (generator → *Impl)
├── Infrastructure/            -- IAuditingProvider, ITenantProvider, entity listener
│   └── SampleDatabaseHost.cs  -- connection factory from config (Compose SQL Server or SQLite)
├── AppDb.cs                   -- SQL Server + SQLite DDL bootstrap
├── docker-compose.yml         -- SQL Server 2022 on localhost:14333
├── smoke-test.sh              -- curl all demo endpoints; writes responses.txt
├── DemoEndpoints.cs           -- grouped minimal API demos
├── Program.cs                 -- DI: AddDapperXRepositories + providers; schema bootstrap
└── appsettings.json           -- ConnectionStrings:Default + DapperX:DatabaseProvider
```

***

## 2.11 Tests

Compile-time provider is **one per test assembly** (`DapperXDatabaseProvider` MSBuild property). Shared generation tests link via `DapperX.Tests.Shared/ProviderGenerationTests.props`. Integration tests use **Testcontainers** (Docker required).

```
tests/
│
├── README.md                              -- provider matrix; dotnet test filters (see EPIC 26a legend in Tasks.md)
├── DapperX.Tests.ProviderTestProject.props
├── DapperX.IntegrationTests.ProviderTestProject.props
│
├── DapperX.Tests.Shared/                  -- no test-runner csproj; linked into four compile projects
│   ├── GeneratedSourceReader.cs
│   ├── ProviderExpectations.cs            -- AssertSqlServerOrderByBeforeOffset, AssertIdentityInsertExcludesId
│   ├── ProviderGenerationTests.props      -- Fixtures/**/*.cs + Generation/**/*.cs
│   ├── Fixtures/                          -- Matrix* entities (catalog, lifecycle, relations, graph, CPQL, proc)
│   └── Generation/
│       ├── UpsertGenerationMatrixTests.cs
│       ├── SlicePagingMatrixTests.cs
│       ├── MatrixPagingAndCrudTests.cs
│       ├── MatrixLifecycleAndFilterTests.cs
│       ├── MatrixRelationsAndGraphTests.cs
│       └── MatrixCpqlSequenceAndComplianceTests.cs
│
├── DapperX.Tests/                         -- SqlServer (primary); ~418 tests; `single-project` + SqlServer-only
│   ├── Fixtures/                          -- entity + repository definitions for generator
│   │   -- TenantRegionUserEntity.cs (soft delete + parameterized global filter + tenant)
│   ├── IdentityInsertRegressionTests.cs   -- smoke-fix: identity INSERT omits @Id
│   ├── PagedSortSqlTests.cs               -- smoke-fix: ApplySortToPagedSql replaces ORDER BY before OFFSET
│   ├── SqlServerTableHintTests.cs         -- smoke-fix: lock hints after FROM table, before WHERE
│   ├── *GenerationTests.cs, *Tests.cs      -- full list: repo `tests/DapperX.Tests/*Tests.cs` (~66 files)
│   │   -- Core / mapping: MappingValidationGenerationTests, RepositoryGenerationTests, RulesComplianceTests
│   │   -- CPQL: CpqlParserTests, CpqlTranslatorTests, CpqlSemanticValidatorTests, CpqlScalarSnapshotTests, CpqlGenerationTests, CpqlFilterTests, EpicFollowUpTests
│   │   -- Query: QueryGenerationTests, QueryBuilderTests, DerivedQueryGenerationTests, DerivedQueryValidatorTests, MethodNameParserTests
│   │   -- Lifecycle / behavior: LifecycleTests, AuditingTests, SoftDeleteTests, SoftDeleteBypassGenerationTests, MultiTenancyTests, GlobalFilterGenerationTests
│   │   -- Relations / graph: LazyLoadingTests, LazyLoaderGenerationTests, LazyMapGenerationTests, BatchRelationshipLoaderGenerationTests, GraphEpicTests, GraphCascadeGenerationTests, ManyToManyGenerationTests
│   │   -- Concurrency / locking: ConcurrencyGenerationTests, ConcurrencyAndLockingTests, LockingGenerationTests
│   │   -- Features: UpsertGenerationTests, GeneratedColumnGenerationTests, SecondaryTableTests, PrimaryKeyJoinColumnGenerationTests, ElementCollectionGenerationTests, NamedEntityGraphGenerationTests, StoredProcedureGenerationTests, BulkInsertGenerationTests, SequenceGenerationTests, FormulaColumnGenerationTests, ColumnTransformerGenerationTests, EmbeddableGenerationTests, ConverterGenerationTests, AssociationOverrideGenerationTests, CompositeKeyGenerationTests, CompositeKeyDiagnosticTests
│   │   -- Compliance: Section23ComplianceTests, IndexNonRegressionTests, UniqueConstraintGenerationTests, ImmutableGeneratorContractTests, LazyLoadingContractTests
│   │   -- Paging / options: GetAllAsyncGenerationTests, DapperXOptionsTests, CompileTimeDatabaseProviderTests, MultiDatabaseProviderTests
│   │   -- Logging / errors: LoggingTests, LoggingIntegrationTests, ExecutableSqlFormatterTests, SqlExecutionExceptionTests, ErrorHandlingGenerationTests, TransactionSupportTests, InvalidSortExceptionTests
│   │   -- Batch: RepositoryBatchGenerationTests, BatchChunkerTests, MutatingMethodGenerationTests
│
├── DapperX.Tests.PostgreSql/              -- matrix-4 (~32 tests); imports Shared via props
├── DapperX.Tests.MySql/                   -- matrix-4 (~32 tests)
├── DapperX.Tests.Sqlite/                  -- matrix-4 (~32 tests) + SqliteUnsupportedFeatureTests.cs
│
├── DapperX.IntegrationTests.Shared/
│   ├── SqlExecutionCountFixture.cs
│   ├── SqliteGuidTypeHandler.cs            -- Dapper Guid handler for Sqlite TEXT tenant_id columns
│   ├── IntegrationFixtures.props
│   ├── IntegrationScenarios.props
│   ├── Fixtures/                         -- Integ* entities (+ IntegAdvancedEntities, proc)
│   │   -- IntegTenantRegionUser in IntegRuntimeExtrasEntities.cs (tenant + parameterized filter + soft delete)
│   ├── IntegrationProcedureBootstrap.cs
│   ├── Scenarios/IntegAllProvidersTests.cs
│   ├── Scenarios/IntegAdvancedFeaturesTests.cs
│   ├── Scenarios/IntegStoredProcedureTests.cs (not Sqlite)
│   ├── Scenarios/IntegLockTimeoutTests.cs (not Sqlite)
│   └── Scenarios/IntegSqliteDialectTests.cs
│
├── DapperX.IntegrationTests.SqlServer/    -- Testcontainers; SqlServerContainerHealthTests.cs
├── DapperX.IntegrationTests.PostgreSql/   -- linked scenarios (Docker)
├── DapperX.IntegrationTests.MySql/        -- linked scenarios (Docker)
├── DapperX.IntegrationTests.Sqlite/       -- linked scenarios (in-memory)
│
└── DapperX.IntegrationTests/              -- deprecated (not in slnx; stub removed)
```

**CI:** [`.github/workflows/ci.yml`](.github/workflows/ci.yml) — `compile-tests` (`FullyQualifiedName!~IntegrationTests`); `integration-tests` (Testcontainers; Docker for SqlServer/PostgreSql/MySql).

***

# 3. Key Data Flow

***

## Compile Time

```
Entity classes + Repository interfaces
           ↓
    DapperX.Generator
           ↓
    ┌─────────────────────────────────────────────────────────────┐
    │  MetadataBuilder → EntityModel                              │
    │  MappingValidator + CompositeKeyValidator → Diagnostics     │
    │  GraphBuilder → DAG + TopologicalSorter                     │
    │  PropertyNameValidator → reserved keyword warnings           │
    │  DerivedQueryGenerator + PropertyFirstResolver → SQL        │
    │  CpqlParser → CpqlValidator → CpqlTranslator                │
    │  SortLookupGenerator → ORDER BY fragments                   │
    │  ColumnResolverEmitter → ResolveColumn() per entity         │
    │  AuditingGenerator → field injection in INSERT/UPDATE       │
    │  SoftDeleteGenerator → DELETE→UPDATE SET (provider deleted_at); SqlBuilder SELECT filters │
    │  TenancyGenerator → tenant WHERE + runtime @tenantId param wiring        │
    │  FormulaEmitter → formula expressions in SELECT             │
    │  ColumnTransformerEmitter → Read/Write SQL expressions       │
    │  GeneratedColumnSqlBuilder → OUTPUT/RETURNING + re-SELECT   │
    │  GlobalFilterGenerator → FILTER_* constants + appenders     │
    │  SecondaryTableGenerator → per-table SQL + SELECT JOIN      │
    │  PrimaryKeyJoinColumnGenerator → shared-PK JOIN + Id assign │
    │  UpsertGenerator → MERGE / ON CONFLICT SQL per provider     │
    │  ElementCollectionGenerator → collection table SQL + lifecycle │
    │  LazyLoaderGenerator → element collection GetAsync wiring       │
    │  StoredProcedureGenerator → SP call + OUT param wrappers    │
    │  NamedEntityGraphGenerator → graph SQL (+ filters applied)  │
    │  RepositoryEmitter → inline SQL methods                     │
    │  ExecutionPlanEmitter → graph plans                         │
    │  LifecycleEmitter → hook invocations                        │
    └─────────────────────────────────────────────────────────────┘
           ↓
    Generated Code (per [Entity] class → [Repository] interface pair):
      - Sealed `{Name}RepositoryImpl : DapperXRepositoryBase<TEntity,TId> [, IInterface]` (no partial)
      - Only SQL string property overrides (compile-time literals) + ResolveColumn switch + derived query methods
      - DapperXRepositoryBase<TEntity,TId> (in Runtime) provides all Dapper call logic — never repeated per entity
      - One `DapperXServiceCollectionExtensions.g.cs` per compilation — registers all entities' Impl classes for ASP.NET Core DI
        (both custom IXxxRepository and IRepository<TEntity,TId> resolve to the same scoped Impl instance)
      - Inline SQL string literals (auditing, soft-delete, tenancy, global filters, column transformers baked in)
      - Paired soft-delete SQL literals per SELECT method: one WITH filter (default), one WITHOUT (IncludeDeleted)
      - GetAllAsync() / GetAllAsync(Sort) / GetAllAsync(Pageable) / GetAllAsync(Sort, Pageable) variants returning Page<T>
      - GetAllSliceAsync(Pageable) / GetAllSliceAsync(Sort, Pageable) variants returning Slice<T> (no COUNT query)
      - DeleteAllByIdAsync SQL literal (DELETE WHERE id IN @ids)
      - Sort lookup switch expressions (ORDER BY fragments)
      - ResolveColumn(propertyName) switch for WhereTranslator (no reflection)
      - Named entity graph SQL switch (filters included)
      - FILTER_{NAME} compile-time constants for each [GlobalFilter]
      - Re-SELECT SQL literals for [Generated] columns (post-INSERT and/or post-UPDATE)
      - Graph execution plan
      - Lifecycle + listener invocations
      - Projection column lists
      - Formula and column transformer expressions in SELECT
      - HardDeleteAsync (for [SoftDelete] entities only)
      - UpsertAsync / UpsertManyAsync provider-specific SQL
      - Element collection load/insert/delete SQL
      - Stored procedure call wrapper with OUT param capture
      - Secondary table LEFT JOINs in SELECT; split INSERT/UPDATE/DELETE per table (topological order)
      - PrimaryKeyJoinColumn shared-PK JOIN condition; child Id pre-assignment before INSERT
      - LazyMap<K,V> load SQL (identical to OneToMany SELECT; grouping in-memory via MapLoader)
      - Load{Map}ForManyAsync SQL (WHERE fk IN @parentIds; same pattern as Load{Collection}ForManyAsync)
      - Slice<T> SQL (SELECT with pageSize+1 rows; hasNext determined at runtime from count)
```

***

## Runtime

```
Developer calls repository method
           ↓
    (optional) IncludeDeleted flag selects WITH-filter or WITHOUT-filter SQL literal
    (optional) AsSlice() flag selects pageSize+1 template instead of pageSize + COUNT template
    (optional) Sort switch selects ORDER BY fragment from pre-generated lookup
    (optional) WhereTranslator builds WHERE clause from expression tree
    (optional) Pageable appends OFFSET / FETCH template (or pageSize+1 if AsSlice)
    (optional) generated `ApplyGlobalFilters()` delegate appends active FILTER_* constants from `QueryRuntimeConfig`
           ↓
    baseSql [+ orderByFragment] [+ whereFragment] [+ pagingFragment] [+ activeFilterFragments]
           ↓
    if LogSql = true:
        build DapperXLogEntry {
            MethodName  = compile-time literal (baked in by generator)
            Sql         = final assembled SQL
            Parameters  = param dict (if LogParameters = true)
            ExecutableSql = ExecutableSqlFormatter.Format(sql, params) (if LogExecutableSql = true)
        }
        DapperXOptions.Logger(entry)   ← fires BEFORE Dapper call
           ↓
    DbExecutor → Dapper (QueryAsync / ExecuteAsync)   ← always parameterized
           ↓
    Database
           ↓
    (optional) Converter read path
    (optional) PostLoad lifecycle hook
    (optional) if AsSlice: trim last element if result.Count > pageSize; set HasNext flag
           ↓
    Strongly typed result (T, IEnumerable<T>, Page<T>, or Slice<T>)
```

***

# 4. Dependency Flow

```
DapperX.Core
DapperX.Abstractions
       ↑
DapperX.Query       → Core, Abstractions
DapperX.Relations   → Core, Abstractions
DapperX.Lifecycle   → Core, Abstractions
DapperX.Batching    → Core, Abstractions
DapperX.Provider    → Core, Abstractions
       ↑
DapperX.Runtime     → Provider, Batching, Abstractions, Query, Core
       ↑
DapperX.Generator   → Core only (Roslyn SemanticModel — never Runtime)
```

***

# 5. Design Rules

All rules below implement the formal guarantees in Requirements.md Section 1 (Rules A–E) and the constraints in Section 23.

**When to add a numbered rule here:** Only when a specific implementation detail needs calling out that is not immediately obvious from Rules A–E. Do not add a numbered rule purely to restate an existing Rule A–E — the coverage table already establishes the link.

| Requirement source | Covers |
|---|---|
| **Req Rule A** — SQL always compile-time | Rules 3, 5, 6, 7, 9, 11, 13, 14, 15, 18, 19, 20 below |
| **Req Rule B** — No runtime reflection on entity types | Rules 1, 2, 7 below |
| **Req Rule C** — Runtime data ops are not SQL violations | Rules 16, 17 below |
| **Req Rule D** — Stateless = no cross-call state | Rules 10, 12 below |
| **Req Rule E** — Informational annotations produce no SQL | Requirements.md Rule E is self-contained; no separate numbered rule needed |
| **Section 23 Constraints** — Code generation system requirements | Rules 1, 2, 3, 7, 19 below |

***

## Rule 1
Generator NEVER depends on runtime execution. It reads entity definitions via Roslyn only. *(Implements Rule B)*

## Rule 2
Runtime NEVER uses reflection. All type knowledge comes from generated code. *(Implements Rule B)*

## Rule 3
All SQL is generated as string literals (or selected from pre-generated literals at runtime for Sort). No SQL is assembled from user-controlled input. *(Implements Rule A)*

## Rule 4
Each module has a single responsibility. No module reaches outside its layer.

## Rule 5
`DatabaseProvider` is a compile-time constant: MSBuild `DapperXDatabaseProvider` (with `CompilerVisibleProperty`) or `[DapperXDatabaseProvider]` on the consuming assembly; resolved by `CompileTimeDatabaseProvider` in the generator (default SqlServer). The generator selects provider-specific SQL at build time — no runtime branching on SQL dialect. *(Implements Rule A)*

## Rule 6
`Sort` ORDER BY fragments are compile-time literals selected at runtime via a switch. Only `[Sortable]`-marked properties are valid; any other column throws `InvalidSortException`. *(Implements Rule A)*

## Rule 7
Query builder `WHERE` / `ORDER BY` / paging fragments append parameterized templates. Column names come from the generated `ResolveColumn(propertyName)` switch — never from `System.Reflection.MemberInfo` or user input. *(Implements Rules A and B)*

## Rule 8
`[Immutable]` entities get SELECT-only repositories. The generator emits a `Diagnostic` compile error if any mutating method is declared or called on an immutable entity.

## Rule 9
Auditing field population (`[CreatedDate]`, `[LastModifiedDate]`, `[CreatedBy]`, `[LastModifiedBy]`), soft-delete rewriting, tenancy filter injection, and named entity graph SQL are all resolved at compile time — baked into the generated SQL string literals. No runtime branching on these behaviours. *(Implements Rule A)*

## Rule 10
`SequenceGenerator.AllocationSize` is not supported — block allocation requires cross-call state. One DB call per insert for sequence strategy. Developers needing allocation optimization must inject `ISequenceAllocator`; DapperX stays stateless. *(Implements Rule D)*

## Rule 11
`FindAllByIdAsync` is not generated for composite-key entities — a multi-row composite-key lookup requires dynamic SQL that grows with the collection. Developers use derived query methods with IN on individual key columns instead. *(Implements Rule A)*

## Rule 12
`LazyCollection`, `LazyReference`, and `LazyMap` do not implement `Reload()`. Entity instances are read-once value objects. For fresh data, call the repository to obtain a new instance. The per-instance load cache is a performance optimization within a single operation, not a long-lived state container. *(Implements Rule D)*

## Rule 13
Global custom filters (`[GlobalFilter]`) are compile-time SQL fragment constants (`private static readonly string FILTER_{Name}` on `{Entity}RepositoryImpl`; child-entity filters use `file static class {Child}Filters` when batch-loaded from a parent repo). `MetadataBuilder.ResolveGlobalFilters` walks `[MappedSuperclass]` bases (subclass filter names override). Runtime: `ApplyGlobalFilters(sql)` appends active fragments via `IDapperXOptions.IsFilterActive`; same helper wraps UPDATE/DELETE, CPQL, derived queries, and `Load{Collection}ForManyAsync` child SELECTs. *(Implements Rules A and C)*

## Rule 14
`[Generated]` columns are excluded from INSERT/UPDATE SQL at compile time. The re-SELECT after mutation is a separate compile-time SQL literal — no reflection or runtime metadata lookup is used to determine which columns to re-fetch. *(Implements Rules A and B)*

## Rule 15
`[SecondaryTable]` INSERT follows primary-first order; DELETE follows secondary-first order (to satisfy FK constraints). Both are compile-time SQL literals — **no runtime determination of execution order**; the generator encodes the topological order at compile time. *(Implements Rule A)*

## Rule 16
`[PrimaryKeyJoinColumn]` shared-PK JOIN condition (`ON child.id = parent.id`) is a compile-time string literal. Child Id assignment before INSERT is a **runtime data operation** (not SQL construction) — it assigns a value to a property, not an SQL fragment, and does not break the compile-time SQL rule. *(Implements Rules A and C)*

## Rule 17
`LazyMap<K,V>` load SQL is identical to `[OneToMany]` — a compile-time literal. Dictionary grouping by `[MapKey]` column is **in-memory LINQ** — no dynamic SQL. `Load{Map}ForManyAsync` follows the same `WHERE fk IN @parentIds` compile-time pattern as `Load{Collection}ForManyAsync`. *(Implements Rules A and C)*

## Rule 18
`LockMode.PessimisticRead` emits `FOR SHARE` / `WITH (HOLDLOCK, ROWLOCK)` as a compile-time string literal per provider (MySQL uses `FOR SHARE` in `QueryLockSuffix` and derived queries). SQLite: **DPX037** when a derived method declares `LockMode`; `IQuery.WithLock` pessimistic modes throw at runtime via `QueryLockSuffix` — not supported. *(Implements Rule A)*

## Rule 19
The generator must produce all 32 items in the Requirements.md Section 23 "Must generate" checklist. Any omission is a spec violation. The checklist includes: `GetAllAsync` overloads, `GetAllSliceAsync` overloads (pageSize+1 template, no COUNT), `DeleteAllByIdAsync`, `IncludeDeleted` paired SQL literals, Sort lookup tables, `ResolveColumn()` switch, `FILTER_*` constants, lifecycle invocations, auditing/soft-delete/tenancy injection, formula/ColumnTransformer/Generated SQL, secondary table split SQL, `[PrimaryKeyJoinColumn]` JOIN + Id assign, `LazyMap` load SQL + batch loader, element collection SQL, named entity graph switch, upsert SQL, stored procedure wrappers, full CPQL translation, `MethodName` literals, SQLite Diagnostic errors, `[Index]` → `IndexMetadata` (no SQL). *(Implements Section 23 Constraints)*

## Rule 21 (updated)
`DapperXRepositoryBase<TEntity, TId>` provides all Dapper call logic once. The generator emits only SQL string property overrides (compile-time literals) and derived query method bodies. Dapper call code must never be duplicated in generated classes. *(Implements Rule A — SQL strings; Rule 4 — single responsibility)*

## Rule 20
`Slice<T>` return type emits one compile-time SQL literal with `pageSize+1` rows (provider-specific: `FETCH FIRST n ROWS ONLY` for SQL Server; `LIMIT n` for PostgreSQL / MySQL / SQLite). `HasNext` is determined at runtime by arithmetic on result count (`result.Count > pageSize`) — no dynamic SQL. *(Implements Rules A and C)*

Note: `[Index]` and `[UniqueConstraint]` producing no SQL is covered by Requirements.md Rule E directly — no separate numbered rule is needed here.

***

# 6. Build Order

## Step 1
`DapperX.Core` + `DapperX.Abstractions`

## Step 2
`DapperX.Query`

## Step 3
`DapperX.Generator` (basic repository + SQL + CPQL + derived queries)

## Step 4
`DapperX.Runtime`

## Step 5
`DapperX.Relations`

## Step 6
`DapperX.Batching`

## Step 7
`DapperX.Provider`

## Step 8 (verification — EPIC 26a)

`DapperX.Tests.Shared` + `ProviderGenerationTests.props` (no csproj)

## Step 9

Four compile-time test projects: `DapperX.Tests` (SqlServer primary), `DapperX.Tests.PostgreSql`, `DapperX.Tests.MySql`, `DapperX.Tests.Sqlite`

## Step 10

`DapperX.IntegrationTests.Shared` + four `DapperX.IntegrationTests.{Provider}` projects (Testcontainers / in-memory Sqlite)

## Step 11

`.github/workflows/ci.yml` — compile and integration test jobs

***

# 7. Final Insight

> This structure ensures:
>
> * Generator handles all complexity at compile time — entity mapping, CPQL translation, derived query parsing, sort lookup generation, graph planning
> * Runtime remains minimal and fast — selects pre-built SQL, appends parameterized fragments, executes via Dapper
> * Features scale without breaking architecture — new attributes in Core, new emitters in Generator, new dialect support in Provider
