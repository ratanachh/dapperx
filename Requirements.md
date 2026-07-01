# DapperX – Full Detailed Requirements and Feature Specification

***

# 1. Core Design Principles

## Requirements

* All operations must be explicit (no implicit database calls)
* No runtime reflection allowed
* No change tracking system
* No identity map or caching layer
* Execution must be stateless
* All SQL must be predictable and visible
* All heavy logic must be resolved at compile time
* Runtime must only execute prepared logic
* System must prioritize batch execution over per-entity execution

## Design Rule Compliance for All Features

Every feature in this specification must satisfy these four compile-time and stateless guarantees:

**Rule A — SQL is always compile-time:**
All SQL (table names, column names, JOIN conditions, WHERE fragments, execution order of multi-statement operations) is determined at compile time and emitted as string literals. Runtime decisions are limited to: selecting between pre-generated literals (Sort switch, IncludeDeleted flag, EntityGraph switch), appending compile-time template fragments (Pageable, global filters, lock hints), and passing `@param` values.

**Rule B — No runtime reflection on entity types:**
All property→column resolution uses the generated `ResolveColumn(propertyName)` switch. No `MemberInfo`, `Type.GetProperties()`, `Activator.CreateInstance()`, or `dynamic` in generated or runtime code.

**Rule C — Runtime data operations are not SQL violations:**
Grouping results in-memory (LazyMap dictionary), assigning an Id value before INSERT ([PrimaryKeyJoinColumn]), and conditionally appending a pre-generated SQL fragment (global filters) are all runtime *data* operations — not dynamic SQL construction — and do not break the compile-time SQL rule.

**Rule D — Stateless means no cross-call state:**
Per-instance lazy-load caches (LazyCollection, LazyReference, LazyMap) are intentional bounded exceptions — they hold data on entity instances, not in a global context. Repository methods themselves are stateless: each call is independent, produces new entity instances, and requires no state from prior calls.

**Rule E — Informational annotations produce no SQL:**
`[Index]` and `[UniqueConstraint]` are documentation annotations — the generator stores their metadata but emits zero SQL, DDL, or Diagnostic errors for them. Their presence must never alter any generated repository SQL.

### Relationship Between Formal Rules and Implementation Notes

Rules A–E above are the **authoritative requirements**. The implementation guide (Structures.md) may include numbered implementation rules (e.g., "Rule 7: ResolveColumn() replaces reflection") as developer reminders that a specific feature complies with a formal rule.

**Guideline for adding implementation rules:** Only add a numbered implementation rule in Structures.md when a specific detail needs calling out that is not immediately obvious from Rules A–E. Do not add an implementation rule that merely restates an existing Rule A–E — the coverage table in Structures.md already establishes the link. When in doubt, the formal Rules A–E here are sufficient and no numbered rule is needed.

***

# 2. Entity Mapping

## Features

### Entity Marker

* `[Entity]` attribute marks a class as a managed entity
* Generator only processes classes annotated with `[Entity]`
* Without `[Entity]`, a class is invisible to the generator

***

### Immutable Entity

* `[Immutable]` on an entity class marks it as read-only
* Generator produces only SELECT methods for the entity — no `InsertAsync`, `UpdateAsync`, `DeleteAsync`, `InsertGraphAsync`, etc.
* Immutable entities may still be referenced as children in other entity graphs (read-only side of a relationship)
* Attempting to call a mutating method on an immutable entity produces a `Diagnostic` compile error

***

### Mapped Superclass

* `[MappedSuperclass]` marks an abstract base class that contributes mapped columns to subclasses but is not itself an entity (no own table)
* Common fields (`Id`, `Version`, `CreatedAt`) defined once in a base class
* Generator includes superclass properties in subclass column list
* Superclass cannot be queried directly

***

### Table Mapping

* Map entity to table name
* Default: class name → table name (snake_case by convention)
* Override with `[Table("table_name")]`
* Support `[Table("table_name", Schema = "schema_name")]`
* Support multi-column unique constraints via `[UniqueConstraint(Columns = new[] { "email", "tenant_id" })]` on the entity class (informational, not DDL; multiple attributes allowed)
* Support index documentation via `[Index(Columns = new[] { "email", "tenant_id" }, Name = "idx_users_email", Unique = false)]` on the entity class:
  * Informational only — generator emits **no SQL and no DDL** for `[Index]`
  * Multiple `[Index]` attributes may appear on one entity class
  * Generator stores all `[Index]` attributes as `IndexMetadata` list on `EntityModel`; accessible to schema generation tools and documentation generators
  * `Unique = true` distinguishes unique indexes from non-unique (informational; enforcement is via `[UniqueConstraint]`)
  * `Name` is optional; if omitted, schema tools may auto-generate a name
  * Does NOT affect any generated repository SQL, INSERT, UPDATE, or SELECT

***

### Secondary Table

* `[SecondaryTable("table2", PrimaryKeyJoinColumn = "entity_id")]` maps additional entity properties to a second table that shares the same primary key
* Multiple `[SecondaryTable]` attributes allowed per entity
* Properties in the secondary table are marked with `[Column(Table = "table2")]`
* Generator emits a LEFT JOIN to each secondary table in SELECT SQL as a compile-time literal
* INSERT emits two statements: INSERT into primary table first, then INSERT into secondary table with same PK value — both within the same transaction
* UPDATE emits two statements: UPDATE primary, UPDATE secondary — same transaction
* DELETE emits two statements: DELETE secondary first (to satisfy FK), then DELETE primary — topological order, same transaction

```csharp
[Entity]
[Table("users")]
[SecondaryTable("user_profiles", PrimaryKeyJoinColumn = "user_id")]
public class User {
    [Id] public int Id { get; set; }
    [Column] public string Name { get; set; }                              // primary table
    [Column(Table = "user_profiles")] public string Bio { get; set; }     // secondary table
    [Column(Table = "user_profiles")] public string AvatarUrl { get; set; }
}
```

**Compile-time rule:** All JOIN conditions, table names, and column lists are known at compile time — generator emits all SQL as string literals. Execution order (primary-first INSERT, secondary-first DELETE) is determined at compile time by the generator; there is no runtime branching on execution order. No dynamic SQL. Does not break any rule.

**Stateless rule:** Two INSERT/UPDATE/DELETE statements per operation; both are compile-time literals; no cross-call state required. Does not break any rule.

***

### Column Mapping

* Map property to column
* Default: property name matches column (snake_case by convention)
* Override with `[Column("column_name")]`
* Support full column attributes:
  * `Nullable` — whether column accepts NULL (default: true for reference types)
  * `Insertable` — include in INSERT SQL (default: true)
  * `Updatable` — include in UPDATE SQL (default: true)
  * `Unique` — emit unique constraint hint for single-column uniqueness (informational, not DDL)
  * `Length` — max string length hint for validation
  * `Precision` — total number of digits for decimal/numeric types (informational)
  * `Scale` — digits after decimal point for decimal/numeric types (informational)
  * `ColumnDefinition` — raw SQL type override for provider-specific types
  * `Fetch` — `FetchType.Lazy` excludes the column from the default SELECT; a separate generated `Load{PropertyName}Async(id)` method loads it on demand (useful for large BLOB / TEXT / JSON columns)
  * `Table` — name of the `[SecondaryTable]` this column belongs to; if omitted, column belongs to the primary table

***

### Id Mapping

* Single primary key: exactly one `[Id]` property; must also have `[GeneratedValue]`
* Composite primary key: use `[IdClass(typeof(KeyClass))]` or `[EmbeddedId]` — see Section 36

***

### Id Generation

* `[GeneratedValue(GenerationType.Identity)]` — database auto-increment; generator emits `OUTPUT INSERTED.Id` / `RETURNING id` / `LAST_INSERT_ID()` per provider
* `[GeneratedValue(GenerationType.Sequence, Generator = "seq_name")]` — database sequence; sequence name resolved from `[SequenceGenerator]`; generator emits `NEXT VALUE FOR seq_name` / `nextval('seq_name')` per provider
* `[GeneratedValue(GenerationType.Uuid)]` — generator assigns `Guid.NewGuid()` before insert; no DB round trip
* `[GeneratedValue(GenerationType.Assigned)]` — developer sets key before insert; generator treats it as a regular column

***

### Sequence Generator

* `[SequenceGenerator(Name, SequenceName)]` defines a named sequence configuration
* `Name` — logical name referenced by `[GeneratedValue(Generator = "name")]`
* `SequenceName` — actual sequence name in the database
* Multiple `[SequenceGenerator]` attributes may be defined on the entity or assembly level
* `AllocationSize` is **not supported** — block allocation requires cross-call state, which violates the stateless execution rule. One DB call is made per insert when using sequence generation. Developers who need allocation optimization must inject `ISequenceAllocator`

***

### ISequenceAllocator

* `ISequenceAllocator` is an optional interface the developer injects into the repository
* When present, the generator emits `await _sequenceAllocator.NextAsync("seq_name")` instead of a direct DB call
* The developer provides the implementation — a thread-safe singleton that batches sequence fetches, a Redis counter, or any other strategy
* DapperX itself has no opinion on how the allocator is implemented; it only defines the interface and emits the call
* When `ISequenceAllocator` is not registered and `GenerationType.Sequence` is used, the generator emits a direct `NEXT VALUE FOR` / `nextval()` call per insert

***

### Version Mapping

* One property marked as `[Version]`
* Used for optimistic concurrency control
* Valid types: `int`, `long`, `DateTime`, `DateTimeOffset`
* Version incremented automatically in generated UPDATE SQL
* Version checked in UPDATE and DELETE SQL

***

### Transient Mapping

* `[Transient]` marks a property that must not be mapped to any column
* Generator excludes transient properties from all SQL (SELECT, INSERT, UPDATE)

***

### Sortable Mapping

* `[Sortable]` marks a property as eligible for runtime sort via the `Sort` lookup table
* Generator pre-generates one SQL string literal per `[Sortable]` property × direction (ASC / DESC) for every query method that accepts a `Sort` parameter
* Properties without `[Sortable]` cannot be used as runtime sort columns — passing an unsupported column throws `InvalidSortException` at runtime (not compile time, but no dynamic SQL is produced)

***

### Formula Mapping

* `[Formula("sql_expression")]` maps a property to a SQL expression instead of a column
* Expression is a native SQL fragment (not CPQL) — included verbatim in SELECT
* Formula properties are excluded from INSERT, UPDATE, and WHERE clause generation
* Expression may reference any columns of the entity's table using raw column names
* Read-only by nature — generator enforces `Insertable = false`, `Updatable = false` implicitly

```csharp
[Formula("(SELECT COUNT(*) FROM order_items WHERE order_items.order_id = id)")]
public int ItemCount { get; set; }

[Formula("UPPER(first_name) + ' ' + UPPER(last_name)")]
public string FullName { get; set; }
```

***

## Requirements

* Generator must validate:
  * `[Entity]` marker present
  * Table exists mapping
  * Exactly one `[Id]` present
  * `[GeneratedValue]` present on the `[Id]` property
  * `GenerationType.Sequence` requires a matching `[SequenceGenerator]` by name
  * Version field is valid type
  * `[Formula]` properties must not appear in INSERT, UPDATE, or WHERE generation
  * `[Immutable]` entities must not have mutating methods generated
* Generator must emit a `ResolveColumn(string propertyName)` method per entity — a compile-time switch that maps property names to column names; used by `WhereTranslator` and `OrderByTranslator` at runtime without reflection
* Column list must be precomputed
* Mapping must support nullability
* `[MappedSuperclass]` properties must be merged into subclass column list at compile time
* `Insertable = false` columns must be excluded from INSERT SQL
* `Updatable = false` columns must be excluded from UPDATE SQL
* `[SecondaryTable]` properties (tagged with `[Column(Table = "table2")]`) must be separated into per-table INSERT/UPDATE/DELETE statements; SELECT must emit a LEFT JOIN per secondary table
* `[SecondaryTable]` INSERT: primary table first, then secondary; DELETE: secondary first, then primary — generator validates topological order at compile time
* `[PrimaryKeyJoinColumn]` and `[JoinColumn]` must not appear together on the same `[OneToOne]` property → `Diagnostic` error
* `[MapKey]` must reference an existing column of the child entity; generator validates at compile time
* `[Index]` attributes must be stored as `IndexMetadata` on the entity model — generator emits no SQL, DDL, or Diagnostic errors for `[Index]`; purely informational

***

# 3. Repository System

## Repository Pattern

DapperX uses a **generic base class + generated SQL overrides** pattern. All Dapper call logic lives once in `DapperXRepositoryBase<TEntity, TId>`; the generator only emits the entity-specific SQL string properties and derived query methods. No partial classes. No duplicate boilerplate.

### Two-layer architecture

**Layer 1 — `DapperXRepositoryBase<TEntity, TId>` (in `DapperX.Runtime`, written once):**
Abstract class implementing `IRepository<TEntity, TId>`. All Dapper calls are here. SQL strings are abstract properties — the base class never contains entity-specific knowledge.

**Layer 2 — `{Name}RepositoryImpl` (generated per entity):**
Sealed class extending the base. Provides only compile-time SQL string overrides and derived query method bodies.

```csharp
// DapperX.Runtime — framework base class (written once, not generated)
public abstract class DapperXRepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
{
    protected readonly IDbConnection _connection;

    // All SQL as abstract properties — entity-specific strings provided by generator
    protected abstract string SelectByIdSql  { get; }
    protected abstract string SelectAllSql   { get; }
    protected abstract string InsertSql      { get; }
    protected abstract string UpdateSql      { get; }
    protected abstract string DeleteSql      { get; }
    protected abstract string DeleteByIdSql  { get; }
    // ... paging, slice, sort fragments, count, exists, etc.

    public async Task<TEntity?> GetByIdAsync(TId id, IDbTransaction? tx = null, CancellationToken ct = default)
        => await _connection.QueryFirstOrDefaultAsync<TEntity>(SelectByIdSql, new { id }, tx);
    // ... all IRepository methods call the abstract SQL properties via Dapper
}

// Generator emits — SQL overrides + ResolveColumn + custom derived methods only:
[Entity]
public class Product { ... }

[Repository]
public interface IProductRepository : IRepository<Product, int>
{
    Task<IEnumerable<Product>> FindByCategoryAsync(string category);
}

// Generated: ProductRepositoryImpl.g.cs
public sealed class ProductRepositoryImpl
    : DapperXRepositoryBase<Product, int>, IProductRepository
{
    // Compile-time SQL string overrides (Rule A — all literals)
    protected override string SelectByIdSql => "SELECT id, name, price FROM products WHERE id = @id";
    protected override string InsertSql     => "INSERT INTO products (name, price) VALUES (@Name, @Price)";
    // ...

    // Compile-time switch — no reflection (Rule B)
    public static string ResolveColumn(string prop) => prop switch
    {
        nameof(Product.Name) => "name",
        nameof(Product.Price) => "price",
        _ => throw new UnmappedPropertyException(typeof(Product), prop),
    };

    // Derived query methods — only these need generated SQL bodies:
    public async Task<IEnumerable<Product>> FindByCategoryAsync(string category) { ... }
}
```

### Trigger rules

| Trigger | What is generated |
|---|---|
| `[Entity]` on a class | `{EntityName}RepositoryImpl : DapperXRepositoryBase<TEntity, TId>` with all SQL string overrides |
| `[Repository]` on an interface | Generator additionally implements that interface on the `Impl` class and adds derived query method bodies |

`[Entity]` alone is sufficient to get all base CRUD functionality. `[Repository]` is only needed when the developer wants to declare custom derived query methods.

### `[Repository]` attribute

Placed on a user-defined interface. **No arguments** — entity type is inferred from the `IRepository<TEntity, TId>` generic argument.

```csharp
[Repository]
public interface IProductRepository : IRepository<Product, int>
{
    Task<IEnumerable<Product>> FindByCategoryAsync(string category);
}
```

### Naming convention

Strip leading `I`, append `Impl`:
- `IProductRepository` → `ProductRepositoryImpl`
- `IOrderRepository`  → `OrderRepositoryImpl`
- `ProductRepository` (no `I`) → `ProductRepositoryImpl`

### Dependency Injection — `AddDapperXRepositories()`

The generator emits a **single DI extension method** in `DapperXServiceCollectionExtensions.g.cs` that registers every entity's repository in one call. Because every `{Name}RepositoryImpl` already declares the `[Repository]` interface in its own base-type list, ASP.NET Core DI's standard two-argument `AddScoped<IInterface, TImpl>()` overload works directly — no factory lambdas needed per repository. `IDbConnection` is registered once as scoped from the caller-supplied factory and injected into every `Impl` constructor by the container.

```csharp
// Generated — DapperXServiceCollectionExtensions.g.cs
public static class DapperXServiceCollectionExtensions
{
    public static IServiceCollection AddDapperXRepositories(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        // Register IDbConnection once — all Impl constructors receive it automatically
        services.AddScoped<IDbConnection>(connectionFactory);

        // ProductRepositoryImpl : DapperXRepositoryBase<Product,int>, IProductRepository
        services.AddScoped<IProductRepository, ProductRepositoryImpl>();
        services.AddScoped<IRepository<Product, int>>(sp =>
            sp.GetRequiredService<IProductRepository>());

        // OrderRepositoryImpl : DapperXRepositoryBase<Order,long>, IOrderRepository
        services.AddScoped<IOrderRepository, OrderRepositoryImpl>();
        services.AddScoped<IRepository<Order, long>>(sp =>
            sp.GetRequiredService<IOrderRepository>());

        // ... one block per entity
        return services;
    }
}

// Program.cs — one line to register everything:
builder.Services.AddDapperXRepositories(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("Default")));

// Then inject by either interface:
app.MapGet("/products", async (IProductRepository repo) =>
    await repo.FindByCategoryAsync("Electronics"));
```

**Requirements:**
* Every generated `{Name}RepositoryImpl` declares the `[Repository]` interface in its base-type list — `AddScoped<IXxxRepository, XxxRepositoryImpl>()` works without factory lambdas
* Generator emits one `AddDapperXRepositories()` extension per compilation, covering all `[Entity]`-annotated types
* `IDbConnection` is registered once as scoped; ASP.NET Core DI injects it into every `Impl` constructor automatically
* `IRepository<TEntity, TId>` is forwarded to the same scoped `IXxxRepository` instance via `GetRequiredService`
* If an entity has no `[Repository]` interface, the concrete `{Name}RepositoryImpl` is registered directly and `IRepository<TEntity, TId>` forwards to it
* `connectionFactory` receives `IServiceProvider` so it can resolve scoped connection strings or `IDbConnection` factories
* The generated extension is placed in the root namespace of the consuming assembly

### Requirements

* `[Entity]` triggers generation of `{Name}RepositoryImpl` extending `DapperXRepositoryBase<TEntity, TId>` with all SQL string overrides
* `[Repository]` on an interface adds that interface to the `Impl` class declaration and generates derived query method bodies
* `DapperXRepositoryBase<TEntity, TId>` resides in `DapperX.Runtime`; generated code references it — the Generator itself does NOT depend on Runtime (Rule 1)
* The generated class is `sealed` — not `partial`; all code in one generated file per entity
* Generated file: `{ImplClassName}.g.cs` (e.g., `ProductRepositoryImpl.g.cs`)
* `[Repository]` interface must extend `IRepository<TEntity, TId>` where `TEntity` has `[Entity]` → `Diagnostic` error otherwise
* `[Repository]` may only be placed on interfaces → `Diagnostic` error on non-interface types
* `DapperXRepositoryBase<TEntity, TId>` must remain abstract — the only entity-specific parts are the SQL string properties; Dapper call logic is never repeated per entity

## Features

### CRUD Operations

* `InsertAsync`
* `UpdateAsync`
* `DeleteAsync`
* `GetByIdAsync`
* `GetAllAsync()` — returns all rows with all applicable filters (soft-delete, tenancy)
* `GetAllAsync(Sort sort)` — same as `GetAllAsync()` with runtime sort; uses pre-generated ORDER BY fragment lookup (same pattern as derived method `Sort`)
* `GetAllAsync(Pageable pageable)` — same with paging; appends compile-time `OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY` template
* `GetAllAsync(Sort sort, Pageable pageable)` — both sort and paging combined; returns `Page<T>` (with COUNT)
* `GetAllSliceAsync(Pageable pageable)` — `pageSize+1` fetch; returns `Slice<T>` (no COUNT); one SQL literal
* `GetAllSliceAsync(Sort sort, Pageable pageable)` — combined; returns `Slice<T>`; emits base SQL + ORDER BY fragment + paging template
* `FindAllByIdAsync(IEnumerable<TId>)` — `SELECT … WHERE id IN @ids`; invokes `PostLoad` per result; single-key entities only
* `ExistsByIdAsync(TId)` — `SELECT EXISTS (SELECT 1 FROM … WHERE id = @id)`
* `CountAsync()` — `SELECT COUNT(*) FROM …`
* `DeleteByIdAsync(TId)` — `DELETE FROM … WHERE id = @id`; invokes `PreRemove` / `PostRemove`
* `DeleteAllByIdAsync(IEnumerable<TId>)` — `DELETE FROM … WHERE id IN @ids`; no per-entity lifecycle hooks (set-based, entities not loaded); `PreRemoveBatch` / `PostRemoveBatch` fire; single-key entities only

***

### Upsert Operations

* `UpsertAsync` — insert if not exists, update if exists; uses `MERGE` / `INSERT … ON CONFLICT DO UPDATE` / `INSERT … ON DUPLICATE KEY UPDATE` per provider
* `UpsertManyAsync` — batch upsert for `IEnumerable<T>`

***

### Batch Operations

* `InsertManyAsync`
* `UpdateManyAsync`
* `DeleteManyAsync`

***

### Graph Operations

* `InsertGraphAsync`
* `UpdateGraphAsync`
* `DeleteGraphAsync`

***

## Requirements

* Each method must:
  * Use inline SQL
  * Be generated per entity
  * Be strongly typed
* Must support transaction injection via `IDbTransaction` parameter
* Lifecycle hooks must be invoked
* Version must be validated in update/delete
* `GetByIdAsync` must invoke `PostLoad` hook after mapping
* `UpsertAsync` / `UpsertManyAsync` must use provider-specific syntax generated at compile time; lifecycle hooks (`PrePersist` / `PostPersist` or `PreUpdate` / `PostUpdate`) are not fired — upsert is a set-based operation
* `[Column(Fetch = FetchType.Lazy)]` properties must generate a `Load{PropertyName}Async(TId id)` method per entity with its own SELECT SQL literal containing only that column
* `Load{Collection}ForManyAsync` batch relationship loading methods must be generated per `[OneToMany]` / `[ManyToMany]` relationship
* `Load{Map}ForManyAsync(IEnumerable<TParent>)` batch loading methods must be generated per `[MapKey]` relationship — emits same `SELECT … WHERE fk IN @parentIds` literal as `Load{Collection}ForManyAsync`; after load, groups by FK then by map key column into a `Dictionary<TKey, TValue>` per parent; calls `Set()` on each parent's `LazyMap`
* `GetAllAsync(Sort)` / `GetAllAsync(Pageable)` must use the same pre-generated ORDER BY fragment and paging template patterns as derived query methods — no new SQL construction mechanism
* `GetAllSliceAsync(Pageable)` / `GetAllSliceAsync(Sort, Pageable)` must emit a single SQL literal with `pageSize+1` rows — no COUNT query; `HasNext` determined at runtime from result count; does not break compile-time SQL rule
* `DeleteAllByIdAsync` must not be generated for composite-key entities (same restriction as `FindAllByIdAsync`) — generator emits `Diagnostic` error if declared on a composite-key entity's repository interface
* `GetAllAsync` and `GetAllSliceAsync` overloads must apply all active filters (soft-delete, tenancy, global custom filters)

***

# 4. Query System

## Features

### Filtering

* `Where` (simple expressions only)

***

### Sorting

* `OrderBy`
* `ThenBy`
* `OrderByDescending`
* `ThenByDescending`

***

### Paging

* `Skip`
* `Take`
* `Pageable` parameter carries `PageNumber` and `PageSize` only

**`Page<T>` return type** — total count + content:
* Generator emits two compile-time SQL literals: one COUNT query + one paginated SELECT
* `Page<T>` properties: `Content`, `TotalElements`, `TotalPages`, `PageNumber`
* Use when the UI requires a total page count or total row count

**`Slice<T>` return type** — content + `HasNext` flag, no COUNT query:
* Generator emits one compile-time SQL literal fetching `pageSize + 1` rows
* At runtime: if `result.Count > pageSize` → trim last element, `HasNext = true`; otherwise `HasNext = false`
* `Slice<T>` properties: `Content` (`IReadOnlyList<T>`), `HasNext` (`bool`)
* More efficient than `Page<T>` for infinite-scroll / "load more" patterns — saves one DB round trip per page
* Provider-specific paging SQL for `Slice<T>`:

| Provider | SQL template |
|---|---|
| SQL Server | `OFFSET @offset ROWS FETCH FIRST @n ROWS ONLY` where `@n = pageSize + 1` |
| PostgreSQL | `LIMIT @n OFFSET @offset` where `@n = pageSize + 1` |
| MySQL | `LIMIT @n OFFSET @offset` where `@n = pageSize + 1` |
| SQLite | `LIMIT @n OFFSET @offset` where `@n = pageSize + 1` |

**Compile-time rule:** `Slice<T>` emits one SQL literal with provider-specific `pageSize+1` template; `HasNext` determination is runtime arithmetic on result count — no dynamic SQL. Does not break any rule.

***

### Includes

* `Include` — load a related entity or collection in the same query or split query
* `ThenInclude` — chain includes for deeper relationships

#### Include Modes

* **Joined** (default): generator emits a SQL JOIN; result mapped via multi-column Dapper mapping
* **Split** (`AsSplitQuery`): generator emits a separate SELECT per included relationship; results assembled in memory

***

### Locking

* `WithLock(LockMode.Pessimistic)` — generator emits `WITH (UPDLOCK)` / `FOR UPDATE` / `FOR UPDATE` per provider
* `WithLock(LockMode.Optimistic)` — default; uses Version-based check

***

### Projection

* `Select<TDto>()` — returns `TDto` instead of the full entity
* Generator emits a column list matching the DTO's properties
* DTO must be annotated with `[Projection(From = typeof(TEntity))]`
* Only properties matching column names are selected; no unmapped properties

***

### Execution Modes

* `AsSplitQuery` — split all includes into separate queries
* `AsSlice()` — switches the return type from `Page<T>` to `Slice<T>`; emits `pageSize+1` SQL template instead of COUNT + paginated SELECT; used with `Pageable` parameter

***

### Soft Delete Bypass

* `IncludeDeleted()` — on the query builder, removes the soft-delete filter (`AND is_deleted = 0`) for entities marked `[SoftDelete]`
* Generator pre-generates two SQL literals per SELECT method on a `[SoftDelete]` entity: one with the filter (default), one without (for admin/audit use)
* At runtime, a boolean flag selects between the two literals — no string concatenation
* `IncludeDeleted = true` parameter also supported on derived query methods
* `[SoftDelete]` entities without `IncludeDeleted()` always have the filter — it cannot be bypassed silently

***

## Requirements

* Expressions must be translated into SQL WHERE clause
* Only support safe subset of expressions (no method calls, no dynamic)
* All queries must be parameterized
* Generator provides base SQL, runtime adds filters
* Must avoid dynamic SQL injection risks
* `Include` JOIN mapping must be generated (not runtime-reflected)
* Split query results must be assembled in memory before returning
* Projection DTO must match column names exactly; generator validates at compile time
* Pessimistic lock SQL must be provider-specific and generated at compile time
* `WhereTranslator` and `OrderByTranslator` must resolve property names to column names using the generated `ResolveColumn(propertyName)` per-entity method — never via `System.Reflection.MemberInfo` or `Type.GetProperties()`; expression tree structure parsing (`MemberExpression`, `BinaryExpression`) is permitted but the property→column mapping step must use generated code only
* `AsSlice()` must switch the paging SQL to a `pageSize+1` compile-time template with no COUNT query; the result trimming and `HasNext` assignment are runtime arithmetic only — no dynamic SQL
* `GetAllSliceAsync` must invoke `PostLoad` lifecycle hook per result (same as `GetAllAsync`)
* `Slice<T>` results must apply all active filters (soft-delete, tenancy, global filters) — same as all other SELECT methods

***

# 5. Derived Query Methods

## Features

### Repository Interface

* Developer declares method signatures on an interface extending `IRepository<T>`
* Generator reads method names at compile time, validates them, and emits implementations with inline SQL
* Invalid or unresolvable method names produce a `Diagnostic` compile error

***

### Subject Keywords

Control the query operation type. These are prefixes that appear before `By` in the method name:

| Keyword | SQL operation | Return type | Example |
|---------|---------------|-------------|---------|
| `Find`, `Get`, `Query`, `Search`, `Read` | SELECT | `Task<T>`, `Task<IEnumerable<T>>` | `FindByCategoryAsync` |
| `Stream` | SELECT (unbuffered) | `IAsyncEnumerable<T>` | `StreamByStatusAsync` |
| `Count` | COUNT(*) | `Task<int>` | `CountByCategoryAsync` |
| `Exists`, `Has`, `Contains` | EXISTS | `Task<bool>` | `ExistsByNameAsync` |
| `Delete`, `Remove` | DELETE | `Task` / `Task<int>` rows affected | `DeleteByStatusAsync` |

Write-operation prefixes (entity parameter, no WHERE clause derived from name):

| Keyword | Maps to |
|---------|---------|
| `Insert`, `Add`, `Save`, `Create` | `InsertAsync` |
| `Update`, `Modify` | `UpdateAsync` |

***

### Property Paths

| Pattern | Maps to | Example |
|---------|---------|---------|
| `{Property}` | Column on entity | `FindByNameAsync` |
| `{Embedded}{Property}` | Flattened embeddable column | `FindByAddressCityAsync` → `address_city` |
| `{Nav}{Property}` | Related entity column via single-level JOIN | `FindByCustomerNameAsync` |
| `{Nav}Id` | Foreign key column | `FindByCustomerIdAsync` |

Property names are PascalCase matching entity property names exactly. Underscores are not used as path separators in method names — concatenate embedded prefix and property name directly (`AddressCity`, not `Address_City`).

Navigation property joins are limited to **one level deep**. Multi-hop paths (e.g., `FindByCustomerAddressCityAsync`) are not supported via method name derivation — use `[Query]` with CPQL for multi-hop filtering.

***

### Property Name vs Operator Keyword Conflicts

Entity property names may collide with parsing keywords (`Is`, `Not`, `True`, `False`, `Null`, `Like`, `In`, `And`, `Or`, `Between`, `Before`, `After`, `OrderBy`, `First`, `Top`, `Distinct`, `Count`, `Containing`, `Matches`, etc.). The generator resolves conflicts using a **property-first, longest-match** rule:

#### Resolution algorithm

At each parsing position, the generator:
1. Collects all entity property names (including embedded and navigation prefixes)
2. Tries to match the **longest prefix** that exactly equals a known property name
3. If a property match is found, it is consumed as a property path — keyword parsing is not attempted for that segment
4. If no property match is found, the position is parsed as an operator keyword

This means entity property names always win over same-named keywords when an exact match exists.

#### Examples

| Entity property | Method name | Parsed as |
|---|---|---|
| `IsActive` (bool) | `FindByIsActiveTrueAsync` | `IsActive` (property) + `True` (operator) |
| `IsActive` (bool) | `FindByIsActiveAsync` | `IsActive` (property) + equality |
| `Like` (string) | `FindByLikeAsync` | `Like` (property) + equality |
| `Like` (string) | `FindByNameLikeAsync` | `Name` (property) + `Like` (operator) |
| `Status`, `Null` | `FindByStatusNullAsync` | `Status` (property) + `Null` (null-check operator) |
| `NotDeleted` (bool) | `FindByNotDeletedAsync` | `NotDeleted` (property) + equality |
| `Before` (DateTime) | `FindByBeforeAsync` | `Before` (property) + equality |
| `And` (string) | `FindByAndAsync` | `And` (property) + equality |

#### Unresolvable ambiguity → Diagnostic error

When the longest-match algorithm finds two equally valid interpretations, the generator emits a `Diagnostic` compile error identifying the ambiguity. The developer must use `[Query]` with CPQL:

```
// Ambiguous: 'Not' could be operator OR start of 'NotDeleted' property
// when entity has BOTH a 'Deleted' property AND a 'NotDeleted' property
FindByNotDeletedAsync  →  Diagnostic: ambiguous between Not(Deleted) and NotDeleted
```

#### Reserved property name warning

The generator emits a `Diagnostic` warning when an entity property is named identically to a core keyword (`And`, `Or`, `Not`, `In`, `Between`, `Like`, `True`, `False`, `Null`, `Before`, `After`, `OrderBy`, `First`, `Top`, `Distinct`, `Count`). The property-first rule still resolves single-property cases, but method names combining such properties with other conditions may be unparseable. The warning advises renaming or using `[Query]`.

#### Escape hatch

`[Query]` with CPQL is always available when method name derivation cannot express the required condition:

```csharp
// Instead of ambiguous FindByNotDeletedAndStatusAsync:
[Query("SELECT p FROM Product p WHERE p.NotDeleted = TRUE AND p.Status = :status")]
Task<IEnumerable<Product>> FindActiveByStatusAsync(string status);
```

***

### Comparison Operators

| Keyword | SQL | Example | Parameters |
|---------|-----|---------|------------|
| (none) / `Is` / `Equals` | `=` | `FindByNameAsync`, `FindByNameIsAsync` | 1 |
| `Not` / `IsNot` | `<>` | `FindByCategoryNotAsync` | 1 |
| `GreaterThan` / `IsGreaterThan` | `>` | `FindByPriceGreaterThanAsync` | 1 |
| `GreaterThanEqual` / `IsGreaterThanEqual` | `>=` | `FindByAgeGreaterThanEqualAsync` | 1 |
| `LessThan` / `IsLessThan` | `<` | `FindByStockLessThanAsync` | 1 |
| `LessThanEqual` / `IsLessThanEqual` | `<=` | `FindByRatingLessThanEqualAsync` | 1 |
| `Between` / `IsBetween` | `BETWEEN … AND …` | `FindByPriceBetweenAsync` | 2 |

***

### String Operators

| Keyword | SQL pattern | Example |
|---------|-------------|---------|
| `Like` / `IsLike` | `LIKE @value` | `FindByNameLikeAsync` |
| `Containing` / `Contains` / `IsContaining` | `LIKE '%' + @value + '%'` | `FindByNameContainingAsync` |
| `NotLike` / `IsNotLike` | `NOT LIKE @value` | `FindByNameNotLikeAsync` |
| `NotContaining` / `NotContains` / `IsNotContaining` | `NOT LIKE '%' + @value + '%'` | `FindByNameNotContainingAsync` |
| `StartingWith` / `StartsWith` / `IsStartingWith` | `LIKE @value + '%'` | `FindByEmailStartingWithAsync` |
| `EndingWith` / `EndsWith` / `IsEndingWith` | `LIKE '%' + @value` | `FindByNameEndingWithAsync` |

***

### Collection Operators

| Keyword | SQL | Example | Parameter |
|---------|-----|---------|-----------|
| `In` / `IsIn` | `IN (…)` | `FindByIdInAsync` | `IEnumerable<T>` |
| `NotIn` / `IsNotIn` | `NOT IN (…)` | `FindByStatusNotInAsync` | `IEnumerable<T>` |

***

### Null Checks

| Keyword | SQL | Example |
|---------|-----|---------|
| `IsNull` / `Null` | `IS NULL` | `FindByDeletedAtIsNullAsync` |
| `IsNotNull` / `NotNull` | `IS NOT NULL` | `FindByEmailIsNotNullAsync` |

***

### Boolean Values

| Keyword | SQL | Example |
|---------|-----|---------|
| `True` / `IsTrue` | `= 1` (dialect-aware) | `FindByIsActiveTrueAsync` |
| `False` / `IsFalse` | `= 0` (dialect-aware) | `FindByIsDeletedFalseAsync` |

Note: For properties named `IsXxx`, the property-first longest-match rule resolves most cases correctly — `FindByIsActiveTrueAsync` parses as `IsActive` (property) + `True` (operator) when `IsActive` is an entity property. Remaining ambiguities emit a `Diagnostic` error — use `[Query]` as the escape hatch. See "Property Name vs Operator Keyword Conflicts" above.

***

### Date/Time Operators

| Keyword | SQL | Example |
|---------|-----|---------|
| `Before` / `IsBefore` | `<` | `FindByCreatedAtBeforeAsync` |
| `After` / `IsAfter` | `>` | `FindByCreatedAtAfterAsync` |

***

### Pattern Matching (Regex)

| Keyword | SQL operator | Example |
|---------|-------------|---------|
| `Regex` / `Matches` / `MatchesRegex` / `IsMatches` | `REGEXP` / `~` | `FindByEmailRegexAsync` |

| Provider | Support |
|----------|---------|
| MySQL / MariaDB | Native `REGEXP` |
| PostgreSQL | `~` operator |
| SQL Server | No native support — use `[Query]` → `Diagnostic` error |
| SQLite | Requires `REGEXP` extension loaded at runtime → `Diagnostic` warning; falls back to `[Query(NativeQuery = true)]` |

***

### Logical Operators

| Keyword | SQL | Example |
|---------|-----|---------|
| `And` | `AND` | `FindByCategoryAndIsActiveTrueAsync` |
| `Or` | `OR` | `FindByNameContainingOrCategoryAsync` |

Chain multiple conditions: `FindByCategoryAndPriceBetweenAndIsActiveTrueOrderByPriceAscAsync`

#### Operator Precedence

Method name parsing applies standard SQL precedence: `And` binds tighter than `Or`. Conditions are grouped accordingly:

| Method name | Parsed WHERE clause |
|-------------|---------------------|
| `FindByAAndBOrC` | `(a = @a AND b = @b) OR c = @c` |
| `FindByAOrBAndC` | `a = @a OR (b = @b AND c = @c)` |
| `FindByAOrBAndCAndD` | `a = @a OR (b = @b AND c = @c AND d = @d)` |

**Explicit parenthesized grouping is not possible through method names.** When the required WHERE clause cannot be expressed with standard `And`/`Or` precedence alone — for example:

```sql
-- Group 1: explicit OR group before AND
WHERE (category = @category OR name = @name) AND age = @age AND gender = @gender

-- Group 2: nested OR inside AND
WHERE category = @category AND (name = @name OR (age = @age AND gender = @gender))
```

Use `[Query]` with CPQL instead (see CPQL section).

***

### Ordering Modifiers

| Pattern | SQL | Example |
|---------|-----|---------|
| `OrderBy{Property}Asc` | `ORDER BY … ASC` | `FindAllOrderByNameAscAsync` |
| `OrderBy{Property}Desc` | `ORDER BY … DESC` | `FindByCategoryOrderByPriceDescAsync` |
| `OrderBy…Then…` | Multiple sort keys | `FindAllOrderByNameAscThenPriceDescAsync` |

***

### Distinct

| Pattern | SQL | Example |
|---------|-----|---------|
| `Distinct` | `SELECT DISTINCT` on all selected columns | `FindDistinctByCategoryAsync` |
| `CountDistinct` | `SELECT COUNT(DISTINCT {primaryKey})` | `CountDistinctByCategoryAsync` |

`CountDistinct` counts **distinct entity instances** (by primary key), not distinct values of the filter column. `CountDistinctByCategoryAsync(string category)` emits `SELECT COUNT(DISTINCT id) FROM products WHERE category = @category`. This is equivalent to Spring Data JPA's behavior and is useful when a query involves JOINs that could produce duplicate entity rows.

***

### Case Insensitivity

| Keyword | Effect | Example |
|---------|--------|---------|
| `IgnoreCase` / `IgnoringCase` | `LOWER(col) = LOWER(@val)` | `FindByNameIgnoreCaseAsync` |
| `AllIgnoreCase` / `AllIgnoringCase` | Case-insensitive for all subsequent conditions | `FindByNameAllIgnoreCaseAsync` |

***

### Result Limiting

| Pattern | SQL (dialect-specific) | Example |
|---------|------------------------|---------|
| `First` / `Top` | `FETCH FIRST 1 ROW ONLY` | `FindFirstByCategoryAsync` |
| `First{n}` / `Top{n}` | `FETCH FIRST n ROWS ONLY` | `FindTop10OrderByCreatedAtDescAsync` |

Always pair with `OrderBy` for deterministic results.

***

### Runtime Parameters

Derived methods accept these special runtime parameter types in addition to filter values:

| Parameter type | Effect |
|----------------|--------|
| `Pageable` | Runtime page number + page size **only** → `OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY` appended; SQL structure is compile-time, values are parameters |
| `Sort` | Runtime sort selection from a pre-generated lookup table (see below) |
| `LockMode` | `FOR UPDATE` / `WITH (UPDLOCK)` appended (provider-specific string literal) |

`Pageable` carries **only** `PageNumber` and `PageSize` — no sort information. Sort must come from the method name `OrderBy…` or a `Sort` parameter, never from inside `Pageable`.

**Return `Page<T>`** — total count + content: generator emits two SQL string literals (COUNT query + paginated SELECT).

**Return `Slice<T>`** — content + `HasNext` flag, no COUNT: generator emits one SQL literal with `pageSize+1` rows; `HasNext` is determined at runtime from result count. Use `Slice<T>` instead of `Page<T>` when the total count is not needed (e.g., infinite scroll, cursor-like navigation).

#### Sort — Base SQL + Pre-Generated ORDER BY Fragments

`Sort` does **not** produce dynamic SQL from user input. The generator emits:

1. One **base SQL literal** containing the full SELECT and WHERE for the method
2. One small **ORDER BY fragment literal** per `[Sortable]` property × direction (ASC / DESC)

At runtime, the selected fragment is appended to the base:

```csharp
const string baseSql = "SELECT id, name, price FROM products WHERE category = @category";

var orderBy = (sort.Column, sort.Ascending) switch {
    (nameof(Product.Name),  true)  => " ORDER BY name ASC",
    (nameof(Product.Name),  false) => " ORDER BY name DESC",
    (nameof(Product.Price), true)  => " ORDER BY price ASC",
    (nameof(Product.Price), false) => " ORDER BY price DESC",
    _ => throw new InvalidSortException(sort.Column)
};

return await _connection.QueryAsync<T>(baseSql + orderBy, parameters, transaction);
```

The base SQL is written once — no duplication per sort option. Column names in the ORDER BY fragments are baked in by the generator from entity metadata; they are never derived from user input. Only properties marked `[Sortable]` on the entity are included in the switch; any other column name throws `InvalidSortException`.

***

### Custom Query Attributes

Override name conventions when derivation is insufficient:

| Attribute | Purpose |
|-----------|---------|
| `[Query("…")]` on method | CPQL query (default, `NativeQuery = false`); generator translates to SQL at compile time |
| `[Query("…", NativeQuery = true)]` on method | Raw native SQL string literal; no translation, passed directly to Dapper |
| `[NamedQuery("queryName")]` on method | Matches a named query defined on the entity class |
| `[NamedQuery(Name, Query)]` on entity class | Defines a single reusable CPQL or native SQL string |
| `[NamedQueries(new NamedQuery(…), …)]` on entity class | Defines multiple named queries on one entity; each entry is a `[NamedQuery]` |
| `[StoredProcedure("proc_name")]` on method | Generator emits `EXEC proc_name @p1, @p2` (dialect-aware string literal) — see Stored Procedure section below |
| `[BulkOperation]` on method | Routes write method through the bulk execution path |
| `EntityGraph = "graphName"` parameter | Selects a named entity graph fetch plan; generator pre-generates one SQL literal per graph and selects at runtime via switch — see Section 37 |

***

### Stored Procedure — Advanced Parameters

For stored procedures with output parameters or multiple result sets:

```csharp
[StoredProcedure("sp_process_order",
    OutParameters = new[] { "resultCode", "message" },
    InOutParameters = new[] { "total" })]
Task<ProcResult<int, string>> ProcessOrderAsync(int orderId, decimal total);
```

`ProcParam` / `ParameterMode` remain available as documentation types; attribute metadata uses `OutParameters`, `InOutParameters`, and `ReturnParameter` (valid C# attribute parameter types).

**Parameter modes:**

| Mode | Direction |
|------|-----------|
| `ParameterMode.In` | Input only (default) |
| `ParameterMode.Out` | Output only — value written back after call |
| `ParameterMode.InOut` | Both input and output |
| `ParameterMode.Return` | Scalar return value of the procedure |

**Multiple result sets:**

```csharp
[StoredProcedure("sp_order_report",
    ResultSets = new[] { typeof(OrderSummary), typeof(OrderItem) })]
Task<MultiResult<OrderSummary, OrderItem>> GetOrderReportAsync(int orderId);
```

* Each `ResultSets` type is mapped in order to the procedure's result sets
* `MultiResult<T1, T2>` exposes `.First` (`IEnumerable<T1>`) and `.Second` (`IEnumerable<T2>`)
* Generator emits provider-specific multi-result-set Dapper call at compile time

**Requirements:**
* Generator validates `ProcParam` names and modes at compile time
* OUT and INOUT parameter values are captured from Dapper `DynamicParameters` after execution
* Return type must be `Task<ProcResult<…>>` for OUT params or `Task<MultiResult<…>>` for multiple result sets
* All provider-specific call syntax (SQL Server `EXEC`, PostgreSQL `SELECT * FROM`, MySQL `CALL`) generated as string literal

***

### CPQL — Compile-Time Persistent Query Language

CPQL is DapperX's entity-oriented query language, modelled after JPQL. It is processed **entirely at compile time** by the Roslyn source generator — translated to provider-specific SQL string literals. No CPQL string exists at runtime.

***

#### Grammar

```
statement      ::= with_clause? select_statement
                 | with_clause? update_statement
                 | delete_statement

-- WITH (CTE)
with_clause       ::= WITH RECURSIVE? cte_def (, cte_def)*
cte_def           ::= cte_name AS ( select_statement )
cte_name          ::= identifier                             -- arbitrary name, used in FROM / JOIN

-- SELECT
select_statement  ::= select_clause from_clause join_clause* where_clause?
                      groupby_clause? having_clause? orderby_clause?

select_clause     ::= SELECT DISTINCT? select_expr

select_expr       ::= alias                                                  -- all mapped columns
                    | value_expr (, value_expr)*                             -- specific columns / arithmetic
                    | aggregate_expr (, aggregate_expr)*                     -- scalar result
                    | NEW full_type_name ( value_expr (, value_expr)* )      -- constructor projection

-- UPDATE
update_statement  ::= UPDATE EntityName alias SET assignment (, assignment)* where_clause?
assignment        ::= alias.property = value_expr

-- DELETE
delete_statement  ::= DELETE FROM EntityName alias where_clause?

from_clause       ::= FROM (EntityName | cte_name) alias

join_clause       ::= (LEFT)? JOIN alias.relationshipProperty joinAlias
                    | (LEFT)? JOIN cte_name alias ON condition         -- CTE join (explicit ON)

where_clause      ::= WHERE condition

groupby_clause    ::= GROUP BY value_expr (, value_expr)*

having_clause     ::= HAVING condition

orderby_clause    ::= ORDER BY order_item (, order_item)*
order_item        ::= value_expr (ASC | DESC)? (NULLS (FIRST | LAST))?

-- Value expressions (used in SELECT, SET, WHERE, ORDER BY)
value_expr        ::= alias.property
                    | parameter
                    | scalar_function ( value_expr (, value_expr)* )
                    | window_expr                                             -- window function (SELECT and ORDER BY only)
                    | value_expr arithmetic_op value_expr
                    | case_expr
                    | subquery                                                -- scalar subquery (returns single value)
                    | ( value_expr )

arithmetic_op     ::= + | - | * | /

-- CASE / WHEN
case_expr         ::= CASE (value_expr)?
                        (WHEN condition THEN value_expr)+
                        (ELSE value_expr)?
                      END

aggregate_expr    ::= COUNT ( alias | DISTINCT alias.property )
                    | SUM   ( value_expr )
                    | AVG   ( value_expr )
                    | MIN   ( value_expr )
                    | MAX   ( value_expr )

condition         ::= predicate
                    | condition AND condition
                    | condition OR condition
                    | NOT condition
                    | ( condition )
                    | EXISTS ( subquery )
                    | NOT EXISTS ( subquery )

predicate         ::= value_expr comparison_op value_expr
                    | value_expr comparison_op ( subquery )                  -- scalar subquery comparison
                    | value_expr BETWEEN value_expr AND value_expr
                    | value_expr (NOT)? IN ( :paramName )
                    | value_expr (NOT)? IN ( subquery )                      -- subquery IN
                    | value_expr (NOT)? LIKE parameter
                    | alias.property IS (NOT)? NULL
                    | alias.property = TRUE
                    | alias.property = FALSE

comparison_op     ::= = | <> | > | >= | < | <=

parameter         ::= :paramName

-- Subqueries (nested CPQL SELECT; same validation rules as outer query)
subquery          ::= ( select_statement )

-- Cross-provider scalar functions (see Scalar Functions section)
scalar_function   ::= LOWER | UPPER | LENGTH | TRIM | LTRIM | RTRIM | COALESCE | NULLIF | CONCAT
                    | SUBSTRING ( value_expr , value_expr (, value_expr)? )
                    | REPLACE   ( value_expr , value_expr , value_expr )
                    | LEFT      ( value_expr , value_expr )
                    | RIGHT     ( value_expr , value_expr )
                    | MOD       ( value_expr , value_expr )
                    | POWER     ( value_expr , value_expr )
                    | YEAR | MONTH | DAY | CURRENT_DATE | CURRENT_TIMESTAMP
                    | ABS | ROUND | FLOOR | CEIL
                    | CAST ( value_expr AS cpql_type )

cpql_type         ::= STRING | INT | LONG | DECIMAL | DOUBLE | DATE | DATETIME | BOOLEAN

-- Window functions
window_expr       ::= window_function OVER ( window_spec )
window_function   ::= ROW_NUMBER ()
                    | RANK ()
                    | DENSE_RANK ()
                    | PERCENT_RANK ()
                    | NTILE ( integer_literal )
                    | LAG  ( value_expr (, integer_literal (, value_expr)?)? )
                    | LEAD ( value_expr (, integer_literal (, value_expr)?)? )
                    | FIRST_VALUE ( value_expr )
                    | LAST_VALUE  ( value_expr )
                    | SUM   ( value_expr )
                    | AVG   ( value_expr )
                    | COUNT ( value_expr | * )
                    | MIN   ( value_expr )
                    | MAX   ( value_expr )
window_spec       ::= (PARTITION BY value_expr (, value_expr)*)?
                      (ORDER BY order_item (, order_item)*)?
                      (frame_spec)?
frame_spec        ::= (ROWS | RANGE) BETWEEN frame_bound AND frame_bound
frame_bound       ::= CURRENT ROW
                    | UNBOUNDED PRECEDING | UNBOUNDED FOLLOWING
                    | value_expr PRECEDING | value_expr FOLLOWING
```

***

#### SELECT Clause Variants

| Form | Description | Required return type |
|------|-------------|----------------------|
| `SELECT e` | All mapped columns of entity | `Task<T>`, `Task<IEnumerable<T>>`, `Task<Page<T>>`, `IAsyncEnumerable<T>` |
| `SELECT e.Prop1, e.Prop2` | Specific columns | `Task<TDto>` / `Task<IEnumerable<TDto>>` — DTO must be `[Projection]` |
| `SELECT e.Prop1 * :tax` | Arithmetic expression | `Task<TDto>` / `Task<IEnumerable<TDto>>` |
| `SELECT new TDto(e.Prop1, e.Prop2)` | Constructor projection | `Task<TDto>` / `Task<IEnumerable<TDto>>` — no `[Projection]` needed |
| `SELECT COUNT(e)` | Row count | `Task<int>` / `Task<long>` |
| `SELECT COUNT(DISTINCT e.Prop)` | Distinct value count | `Task<int>` / `Task<long>` |
| `SELECT SUM(e.Prop)` | Sum | `Task<decimal>` / `Task<int>` / `Task<long>` |
| `SELECT AVG(e.Prop)` | Average | `Task<double>` / `Task<decimal>` |
| `SELECT MIN(e.Prop)` / `SELECT MAX(e.Prop)` | Min / max | Matches property type |

Generator validates return type compatibility at compile time and emits `Diagnostic` on mismatch.

***

#### JOIN Syntax

**Explicit JOIN** — use relationship property name, not table name:

```
JOIN alias.RelationshipProperty joinAlias         -- INNER JOIN
LEFT JOIN alias.RelationshipProperty joinAlias    -- LEFT OUTER JOIN
```

**Implicit JOIN via path expression** — path expressions in WHERE / ORDER BY / SELECT automatically generate INNER JOINs without a JOIN clause:

```
// Explicit:
[Query("SELECT p FROM Product p JOIN p.Category c WHERE c.Name = :name")]

// Implicit (identical SQL output):
[Query("SELECT p FROM Product p WHERE p.Category.Name = :name")]
```

* If the same relationship is referenced multiple times, one JOIN is emitted
* Implicit joins always use INNER JOIN; use explicit `LEFT JOIN` for outer joins
* No hard depth limit — the generator traverses the relationship graph to any depth at compile time
* Very deep chains (5+ hops) are valid CPQL but will produce complex SQL; developer is responsible for query performance

***

#### Named Parameters

* Syntax: `:paramName` inside CPQL string
* Each `:paramName` must exactly match a method parameter name (case-sensitive)
* Generator validates all `:paramName` references are bound to a method parameter at compile time
* Unbound parameters → `Diagnostic` compile error
* Method parameters with no `:paramName` reference → `Diagnostic` compile warning
* Translated to `@paramName` in emitted SQL for Dapper
* Collection parameters (`IEnumerable<T>`) are translated to provider-specific parameterized IN list (no string concatenation)

***

#### Constructor Expressions

```csharp
[Query("SELECT new ProductSummary(p.Name, p.Price, p.Category.Name) FROM Product p WHERE p.IsActive = TRUE")]
Task<IEnumerable<ProductSummary>> FindActiveSummariesAsync();
```

* `ProductSummary` must have a constructor whose parameter types match the projected properties in order
* Generator validates constructor existence and type compatibility at compile time
* `p.Category.Name` generates an implicit INNER JOIN (counts as 1 hop)
* No `[Projection]` annotation required — constructor expression is self-describing

***

#### WHERE Operators

| CPQL syntax | Emitted SQL |
|-------------|-------------|
| `e.Prop = :val` | `col = @val` |
| `e.Prop <> :val` | `col <> @val` |
| `e.Prop > :val` | `col > @val` |
| `e.Prop >= :val` | `col >= @val` |
| `e.Prop < :val` | `col < @val` |
| `e.Prop <= :val` | `col <= @val` |
| `e.Prop BETWEEN :a AND :b` | `col BETWEEN @a AND @b` |
| `e.Prop IN (:vals)` | `col IN @vals` (Dapper list expansion) |
| `e.Prop NOT IN (:vals)` | `col NOT IN @vals` |
| `e.Prop LIKE :pat` | `col LIKE @pat` |
| `e.Prop NOT LIKE :pat` | `col NOT LIKE @pat` |
| `e.Prop IS NULL` | `col IS NULL` |
| `e.Prop IS NOT NULL` | `col IS NOT NULL` |
| `e.Prop = TRUE` | `col = 1` / `col = TRUE` (dialect-aware) |
| `e.Prop = FALSE` | `col = 0` / `col = FALSE` (dialect-aware) |
| `LOWER(e.Prop) = LOWER(:val)` | `LOWER(col) = LOWER(@val)` |
| `e.Prop * :factor > :threshold` | `col * @factor > @threshold` |
| `e.Prop1 + e.Prop2 < :limit` | `col1 + col2 < @limit` |
| `(condition) AND condition` | `(…) AND …` — explicit grouping |
| `condition AND (condition OR condition)` | `… AND (… OR …)` — nested grouping |

#### WHERE Grouping

Parentheses in CPQL map directly to parentheses in emitted SQL. Use them whenever the required logic cannot be expressed by method name derivation's fixed `And`/`Or` precedence.

```csharp
// Pattern 1: OR group evaluated before AND
// SQL: WHERE (category = @category OR name = @name) AND age = @age AND gender = @gender
[Query("SELECT p FROM Product p " +
       "WHERE (p.Category = :category OR p.Name = :name) AND p.Age = :age AND p.Gender = :gender")]
Task<IEnumerable<Product>> FindByCategoryOrNameWithAgeAndGenderAsync(
    string category, string name, int age, string gender);

// Pattern 2: nested OR inside AND
// SQL: WHERE category = @category AND (name = @name OR (age = @age AND gender = @gender))
[Query("SELECT p FROM Product p " +
       "WHERE p.Category = :category AND (p.Name = :name OR (p.Age = :age AND p.Gender = :gender))")]
Task<IEnumerable<Product>> FindByCategoryWithNameOrAgeGenderAsync(
    string category, string name, int age, string gender);

// Pattern 3: multiple nested groups
// SQL: WHERE (status = @status OR isActive = @flag) AND (price > @min AND price < @max)
[Query("SELECT p FROM Product p " +
       "WHERE (p.Status = :status OR p.IsActive = :flag) AND (p.Price > :min AND p.Price < :max)")]
Task<IEnumerable<Product>> FindByStatusOrFlagWithPriceRangeAsync(
    string status, bool flag, decimal min, decimal max);
```

Parentheses may be nested to any depth. Generator emits them verbatim into the WHERE clause of the translated SQL.

***

#### Arithmetic Expressions

Arithmetic operators `+`, `-`, `*`, `/` are supported in SELECT, SET (UPDATE), WHERE, and ORDER BY:

```
// In WHERE
[Query("SELECT p FROM Product p WHERE p.Price * :taxRate > :threshold")]

// In SELECT (requires [Projection] DTO or constructor expression)
[Query("SELECT new PricedItem(p.Name, p.Price * :taxRate) FROM Product p WHERE p.IsActive = TRUE")]

// In UPDATE SET
[Query("UPDATE Product p SET p.Price = p.Price * :factor WHERE p.Category.Name = :cat")]
```

* Generator validates operand types at compile time — arithmetic on non-numeric properties produces a `Diagnostic` error
* Division by zero is a runtime concern, not compile-time
* Operator precedence follows standard math rules; parentheses are supported

***

#### Bulk UPDATE and DELETE

CPQL supports bulk mutation statements that operate on sets without loading entities:

**UPDATE syntax:**
```
UPDATE EntityName alias SET alias.property = value_expr (, alias.property = value_expr)* [WHERE condition]
```

**DELETE syntax:**
```
DELETE FROM EntityName alias [WHERE condition]
```

**Examples:**
```csharp
// Bulk price adjustment
[Query("UPDATE Product p SET p.Price = p.Price * :factor, p.UpdatedAt = :now WHERE p.Category.Name = :cat")]
Task<int> AdjustPricesByCategoryAsync(decimal factor, DateTime now, string cat);

// Soft delete
[Query("UPDATE Order o SET o.IsDeleted = TRUE, o.DeletedAt = :now WHERE o.Status = :status")]
Task<int> SoftDeleteByStatusAsync(DateTime now, string status);

// Hard delete with filter
[Query("DELETE FROM AuditLog a WHERE a.CreatedAt < :cutoff")]
Task<int> DeleteOldAuditLogsAsync(DateTime cutoff);
```

* Return type must be `Task<int>` — the int is affected row count
* Bulk UPDATE/DELETE bypass entity lifecycle hooks (no `PreUpdate`, `PostRemove`, etc.) — they are set-based operations, not entity operations
* Bulk UPDATE/DELETE do **not** check or increment `[Version]` — no optimistic concurrency; use with care inside a transaction
* Implicit JOINs via path expressions in WHERE are supported (e.g., `p.Category.Name` generates a JOIN)
* Generator emits `UPDATE … FROM … JOIN …` (SQL Server) / `UPDATE … JOIN …` (MySQL) / `UPDATE … FROM … JOIN …` (PostgreSQL) per provider

***

#### Cross-Provider Scalar Functions

A curated set of functions is supported in CPQL. The generator emits the provider-specific equivalent as a string literal:

| CPQL function | SQL Server | PostgreSQL | MySQL | SQLite |
|---------------|------------|------------|-------|--------|
| `LOWER(x)` | `LOWER(x)` | `LOWER(x)` | `LOWER(x)` | `LOWER(x)` |
| `UPPER(x)` | `UPPER(x)` | `UPPER(x)` | `UPPER(x)` | `UPPER(x)` |
| `LENGTH(x)` | `LEN(x)` | `LENGTH(x)` | `LENGTH(x)` | `LENGTH(x)` |
| `TRIM(x)` | `LTRIM(RTRIM(x))` | `TRIM(x)` | `TRIM(x)` | `TRIM(x)` |
| `COALESCE(x, y)` | `COALESCE(x, y)` | `COALESCE(x, y)` | `COALESCE(x, y)` | `COALESCE(x, y)` |
| `ABS(x)` | `ABS(x)` | `ABS(x)` | `ABS(x)` | `ABS(x)` |
| `ROUND(x, n)` | `ROUND(x, n)` | `ROUND(x, n)` | `ROUND(x, n)` | `ROUND(x, n)` |
| `FLOOR(x)` | `FLOOR(x)` | `FLOOR(x)` | `FLOOR(x)` | `FLOOR(x)` |
| `CEIL(x)` | `CEILING(x)` | `CEIL(x)` | `CEIL(x)` | `CEIL(x)` |
| `YEAR(x)` | `YEAR(x)` | `EXTRACT(YEAR FROM x)` | `YEAR(x)` | `CAST(strftime('%Y', x) AS INTEGER)` |
| `MONTH(x)` | `MONTH(x)` | `EXTRACT(MONTH FROM x)` | `MONTH(x)` | `CAST(strftime('%m', x) AS INTEGER)` |
| `DAY(x)` | `DAY(x)` | `EXTRACT(DAY FROM x)` | `DAY(x)` | `CAST(strftime('%d', x) AS INTEGER)` |
| `CURRENT_DATE` | `CAST(GETDATE() AS DATE)` | `CURRENT_DATE` | `CURDATE()` | `date('now')` |
| `CURRENT_TIMESTAMP` | `GETDATE()` | `CURRENT_TIMESTAMP` | `NOW()` | `datetime('now')` |
| `NULLIF(x, y)` | `NULLIF(x, y)` | `NULLIF(x, y)` | `NULLIF(x, y)` | `NULLIF(x, y)` |
| `CONCAT(x, y, …)` | `CONCAT(x, y, …)` | `CONCAT(x, y, …)` | `CONCAT(x, y, …)` | `x \|\| y \|\| …` |
| `LTRIM(x)` | `LTRIM(x)` | `LTRIM(x)` | `LTRIM(x)` | `LTRIM(x)` |
| `RTRIM(x)` | `RTRIM(x)` | `RTRIM(x)` | `RTRIM(x)` | `RTRIM(x)` |
| `SUBSTRING(x, s, n)` | `SUBSTRING(x, s, n)` | `SUBSTRING(x FROM s FOR n)` | `SUBSTRING(x, s, n)` | `SUBSTR(x, s, n)` |
| `REPLACE(x, f, t)` | `REPLACE(x, f, t)` | `REPLACE(x, f, t)` | `REPLACE(x, f, t)` | `REPLACE(x, f, t)` |
| `LEFT(x, n)` | `LEFT(x, n)` | `LEFT(x, n)` | `LEFT(x, n)` | `SUBSTR(x, 1, n)` |
| `RIGHT(x, n)` | `RIGHT(x, n)` | `RIGHT(x, n)` | `RIGHT(x, n)` | `SUBSTR(x, -n)` |
| `MOD(x, y)` | `x % y` | `MOD(x, y)` | `MOD(x, y)` | `x % y` |
| `POWER(x, n)` | `POWER(x, n)` | `POWER(x, n)` | `POWER(x, n)` | `POWER(x, n)` |
| `CAST(x AS STRING)` | `CAST(x AS NVARCHAR)` | `CAST(x AS TEXT)` | `CAST(x AS CHAR)` | `CAST(x AS TEXT)` |
| `CAST(x AS INT)` | `CAST(x AS INT)` | `CAST(x AS INTEGER)` | `CAST(x AS SIGNED)` | `CAST(x AS INTEGER)` |
| `CAST(x AS DECIMAL)` | `CAST(x AS DECIMAL)` | `CAST(x AS NUMERIC)` | `CAST(x AS DECIMAL)` | `CAST(x AS REAL)` |
| `CAST(x AS DATE)` | `CAST(x AS DATE)` | `CAST(x AS DATE)` | `CAST(x AS DATE)` | `date(x)` |
| `CAST(x AS DATETIME)` | `CAST(x AS DATETIME)` | `CAST(x AS TIMESTAMP)` | `CAST(x AS DATETIME)` | `datetime(x)` |

SQLite note: `CONCAT` maps to `||` chained concatenation since SQLite has no `CONCAT` function.

**NULLS FIRST / NULLS LAST in ORDER BY:**

| CPQL | SQL Server | PostgreSQL | MySQL | SQLite |
|------|------------|------------|-------|--------|
| `ORDER BY e.Prop ASC NULLS FIRST` | `CASE WHEN col IS NULL THEN 0 ELSE 1 END, col ASC` | `col ASC NULLS FIRST` | `ISNULL(col), col ASC` | `col ASC NULLS FIRST` (3.30.0+) |
| `ORDER BY e.Prop ASC NULLS LAST` | `CASE WHEN col IS NULL THEN 1 ELSE 0 END, col ASC` | `col ASC NULLS LAST` | `col IS NULL, col ASC` | `col ASC NULLS LAST` (3.30.0+) |

Provider-specific functions not in this list must use `[Query(NativeQuery = true)]`.

***

#### Compile-Time Validation

Generator must validate all of the following and emit `Diagnostic` errors on any violation:

* Entity name in `FROM` / `UPDATE` / `DELETE FROM` clause exists in the entity model
* All property references (`alias.Property`) exist on the referenced entity
* Relationship property in `JOIN` exists and has FK metadata
* All `:paramName` references match method parameter names exactly (outer and subquery share the same namespace)
* Method parameters with no corresponding `:paramName` reference → `Diagnostic` warning
* Return type is compatible with the SELECT clause form (see table above)
* Bulk UPDATE / DELETE return type must be `Task<int>`
* Constructor in `NEW` expression exists with parameter types matching projected column types in order
* Aggregate function return type matches method return type
* `GROUP BY` required when SELECT mixes aggregate and non-aggregate expressions
* `HAVING` used only when `GROUP BY` is present
* `LEFT JOIN` target relationship must allow null (nullable FK or optional reference)
* Arithmetic operands must be numeric types; arithmetic on string/bool/date properties → `Diagnostic` error
* Scalar function argument types must be compatible with the function (e.g., `YEAR` requires date/datetime)
* `CASE` branch value types must all be compatible (same type or implicitly castable); mismatched branch types → `Diagnostic` error
* `CASE` without `ELSE` may return NULL — property type in SELECT must be nullable
* Scalar subquery must project exactly one column (a single `alias.property` or aggregate); multi-column → `Diagnostic` error
* `EXISTS` / `NOT EXISTS` subquery entity name and property references must be valid
* `IN (subquery)` / `NOT IN (subquery)` subquery must project a single column matching the left-hand side type
* Correlated subquery outer alias reference must resolve to the outer query's `FROM` alias
* Nested subqueries (subquery inside subquery) → `Diagnostic` error (not supported)
* `NULLIF(x, y)` — both arguments must be the same type or implicitly compatible
* `CONCAT(…)` — all arguments must be string-compatible types; generator validates at compile time
* `CAST(x AS type)` — source type must be castable to target type; invalid casts → `Diagnostic` error
* `NULLS FIRST` / `NULLS LAST` — emitted as provider-specific equivalent (see scalar function table)
* `SUBSTRING(x, s)` (two-arg form) — generator emits provider-specific omission of length: `SUBSTRING(x, s)` (SQL Server/MySQL), `SUBSTRING(x FROM s)` (PostgreSQL), `SUBSTR(x, s)` (SQLite)
* `LEFT(x, n)` / `RIGHT(x, n)` — operands must be string type; generator validates at compile time
* `MOD(x, y)` — operands must be numeric; generator emits `x % y` for SQL Server and SQLite
* `POWER(x, n)` — operands must be numeric
* Window functions (`ROW_NUMBER`, `RANK`, `SUM OVER`, etc.) — valid in SELECT and ORDER BY only; not valid in WHERE or HAVING; generator emits `Diagnostic` error if used in WHERE/HAVING
* `PARTITION BY` and `ORDER BY` inside `OVER (…)` use entity property names (same resolution as outer query)
* Window functions are supported on all four providers: SQL Server, PostgreSQL, MySQL 8.0+, SQLite 3.25.0+
* Window functions with `ROWS`/`RANGE` frame specs are translated verbatim — generator validates the frame syntax structure but not provider-specific limitations
* Mutating methods (`INSERT`, `UPDATE`, `DELETE` CPQL) on `[Immutable]` entities → `Diagnostic` error
* CTE name must be referenced at least once in the main query or a subsequent CTE body; unreferenced CTE → `Diagnostic` warning
* CTE body SELECT is validated the same as any CPQL SELECT — entity names, property names, parameter bindings all checked at compile time
* CTE name used in FROM of the main query is resolved to the CTE body's projected columns; property references on the CTE alias are validated against those columns
* `RECURSIVE` CTE bodies are not validated beyond syntax — generator emits them as-is; developer is responsible for correctness of recursive member

***

#### Translation Examples

The generator emits different code shapes depending on what runtime flexibility the method requires. The invariant across all patterns: **SQL structure (table names, column names, JOIN conditions) is always a compile-time string literal. Values are always `@param` parameters, never interpolated.**

***

**Pattern 1 — Pure CPQL / derived method (single inline literal)**

No runtime modification needed. SQL is passed directly to Dapper as a string literal.

```csharp
// Interface declaration:
[Query("SELECT new OrderSummary(o.Id, o.TotalAmount, c.Name) " +
       "FROM Order o JOIN o.Customer c " +
       "WHERE o.Status = :status AND o.TotalAmount > :minAmount " +
       "ORDER BY o.CreatedAt DESC")]
Task<IEnumerable<OrderSummary>> FindSummariesByStatusAsync(string status, decimal minAmount);

// Generated implementation (SQL Server):
public async Task<IEnumerable<OrderSummary>> FindSummariesByStatusAsync(
    string status, decimal minAmount, IDbTransaction transaction = null)
{
    return await _connection.QueryAsync<OrderSummary>(
        "SELECT o.id, o.total_amount, c.name " +
        "FROM orders o " +
        "INNER JOIN customers c ON c.id = o.customer_id " +
        "WHERE o.status = @status AND o.total_amount > @minAmount " +
        "ORDER BY o.created_at DESC",
        new { status, minAmount },
        transaction);
}
```

***

**Pattern 2 — Runtime `Sort` parameter (base literal + ORDER BY fragment)**

The generator emits one compile-time base SQL literal for the method's full SELECT/WHERE logic, and one small ORDER BY fragment literal per `[Sortable]` property × direction. At runtime, the selected fragment is appended to the base. This keeps the WHERE clause in one place regardless of how many sort options exist.

Column names in the ORDER BY fragments are baked in by the generator from entity metadata — they are never derived from user input.

```csharp
// Interface declaration:
Task<IEnumerable<Product>> FindByCategoryOrNameWithAgeAndGenderAsync(
    string category, string name, int age, string gender, Sort sort);

// Generated implementation (SQL Server):
public async Task<IEnumerable<Product>> FindByCategoryOrNameWithAgeAndGenderAsync(
    string category, string name, int age, string gender,
    Sort sort, IDbTransaction transaction = null)
{
    const string baseSql =
        "SELECT id, name, price, age, gender FROM products " +
        "WHERE (category = @category OR name = @name) AND age = @age AND gender = @gender";

    var orderBy = (sort.Column, sort.Ascending) switch
    {
        (nameof(Product.Name),  true)  => " ORDER BY name ASC",
        (nameof(Product.Name),  false) => " ORDER BY name DESC",
        (nameof(Product.Price), true)  => " ORDER BY price ASC",
        (nameof(Product.Price), false) => " ORDER BY price DESC",
        _ => throw new InvalidSortException(sort.Column)
    };

    return await _connection.QueryAsync<Product>(
        baseSql + orderBy,
        new { category, name, age, gender },
        transaction);
}
```

***

**Pattern 3 — `Pageable` parameter (append compile-time template, runtime parameter values)**

The base SELECT is a compile-time literal. The paging clause is a compile-time template appended as a string. Only the offset and page size values are runtime parameters — no column names or SQL keywords are interpolated.

```csharp
// Interface declaration:
Task<Page<Product>> FindByCategoryAsync(string category, Pageable pageable);

// Generated implementation (SQL Server):
public async Task<Page<Product>> FindByCategoryAsync(
    string category, Pageable pageable, IDbTransaction transaction = null)
{
    const string basesql =
        "SELECT id, name, price FROM products WHERE category = @category";
    const string pagesql =
        basesql + " ORDER BY id ASC OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
    const string countsql =
        "SELECT COUNT(*) FROM products WHERE category = @category";

    var parameters = new { category, offset = pageable.Offset, pageSize = pageable.PageSize };
    var total   = await _connection.ExecuteScalarAsync<int>(countsql, new { category }, transaction);
    var content = await _connection.QueryAsync<Product>(pagesql, parameters, transaction);
    return new Page<Product>(content, total, pageable);
}
```

***

**Pattern 4 — Query builder `Where()` (append parameterized WHERE at runtime)**

The base SELECT is a compile-time literal. The WHERE clause is constructed at runtime from an expression tree by `WhereTranslator` — column names come from entity metadata (not user input), values are always `@param` parameters. Only the WHERE fragment is runtime-built; the rest of the SQL is static.

```csharp
// Developer usage:
var results = await repo.Query()
    .Where(p => p.Category == category && p.Price > minPrice)
    .OrderBy(p => p.Name)
    .ToListAsync();

// Generated Query() base — compile-time literal:
private const string _baseSql =
    "SELECT id, name, price FROM products";

// Runtime execution path inside QueryExecutor:
public async Task<IEnumerable<T>> ExecuteAsync(
    string baseSql, WhereClause where, OrderByClause orderBy,
    int? skip, int? take, IDbTransaction transaction)
{
    var sql = baseSql;                           // compile-time literal
    if (where != null)    sql += " WHERE "    + where.Sql;    // e.g. "category = @p0 AND price > @p1"
    if (orderBy != null)  sql += " ORDER BY " + orderBy.Sql; // e.g. "name ASC"
    if (skip != null)     sql += " OFFSET @offset ROWS FETCH NEXT @take ROWS ONLY";
    return await _connection.QueryAsync<T>(sql, where?.Parameters, transaction);
}
```

Column names in `where.Sql` and `orderBy.Sql` are resolved from entity metadata at runtime — they are never derived from user input. All filter values are `@param` references in the appended fragment, never interpolated.

***

***

#### CASE / WHEN Expressions

Two forms supported, mirroring JPQL:

**Simple CASE** — compares a single value against branches:
```
CASE e.Status
  WHEN :active   THEN 'Active'
  WHEN :inactive THEN 'Inactive'
  ELSE 'Unknown'
END
```

**Searched CASE** — each WHEN is an independent condition:
```
CASE
  WHEN e.Price > :high THEN 'Premium'
  WHEN e.Price > :mid  THEN 'Standard'
  ELSE 'Budget'
END
```

**Examples:**
```csharp
// CASE in SELECT
[Query("SELECT new ProductTier(p.Name, CASE WHEN p.Price > :high THEN 'Premium' ELSE 'Standard' END) " +
       "FROM Product p WHERE p.IsActive = TRUE")]
Task<IEnumerable<ProductTier>> FindTiersAsync(decimal high);

// CASE in ORDER BY (custom sort priority)
[Query("SELECT p FROM Product p " +
       "ORDER BY CASE WHEN p.IsFeatured = TRUE THEN 0 ELSE 1 END ASC, p.Name ASC")]
Task<IEnumerable<Product>> FindAllFeaturedFirstAsync();

// CASE in WHERE
[Query("SELECT o FROM Order o WHERE CASE WHEN :mode = 'strict' THEN o.Total > :min ELSE o.Total >= 0 END = TRUE")]
Task<IEnumerable<Order>> FindByModeAsync(string mode, decimal min);
```

* `CASE` is a `value_expr` — usable in SELECT, SET (UPDATE), WHERE, ORDER BY
* `ELSE` clause is optional; without it, unmatched branches yield `NULL`
* Generator validates branch value types are compatible at compile time
* Translated to provider-specific SQL (all three providers support standard CASE/WHEN syntax)

***

#### Subqueries

Subqueries are nested CPQL SELECT statements. The same entity/property validation rules apply to the inner query as to the outer.

**Supported subquery forms:**

| Form | Usage | Example |
|------|-------|---------|
| Scalar subquery | Compare against single value in WHERE or SELECT | `WHERE p.Price > (SELECT AVG(p2.Price) FROM Product p2)` |
| EXISTS subquery | Check existence in WHERE | `WHERE EXISTS (SELECT o FROM Order o WHERE o.CustomerId = c.Id)` |
| NOT EXISTS subquery | Check non-existence in WHERE | `WHERE NOT EXISTS (SELECT r FROM Review r WHERE r.ProductId = p.Id)` |
| IN subquery | Set membership in WHERE | `WHERE p.CategoryId IN (SELECT c.Id FROM Category c WHERE c.IsActive = TRUE)` |
| NOT IN subquery | Set exclusion in WHERE | `WHERE p.Id NOT IN (SELECT oi.ProductId FROM OrderItem oi WHERE oi.OrderId = :orderId)` |

**Examples:**
```csharp
// Scalar subquery — products above average price
[Query("SELECT p FROM Product p WHERE p.Price > (SELECT AVG(p2.Price) FROM Product p2)")]
Task<IEnumerable<Product>> FindAboveAveragePriceAsync();

// EXISTS — customers who have placed at least one order
[Query("SELECT c FROM Customer c WHERE EXISTS (SELECT o FROM Order o WHERE o.CustomerId = c.Id)")]
Task<IEnumerable<Customer>> FindCustomersWithOrdersAsync();

// IN subquery — products in active categories
[Query("SELECT p FROM Product p " +
       "WHERE p.CategoryId IN (SELECT c.Id FROM Category c WHERE c.IsActive = TRUE)")]
Task<IEnumerable<Product>> FindInActiveCategoriesAsync();

// Scalar subquery in SELECT (correlated)
[Query("SELECT new CustomerStats(c.Name, (SELECT COUNT(o) FROM Order o WHERE o.CustomerId = c.Id)) " +
       "FROM Customer c")]
Task<IEnumerable<CustomerStats>> FindCustomerStatsAsync();
```

* Subquery parameters share the same `:paramName` namespace as the outer query — all names must be unique and bound to method parameters
* Correlated subqueries (referencing outer alias in inner WHERE) are supported
* Subquery itself may not contain another subquery (no nested subqueries in initial release)
* `EXISTS` / `NOT EXISTS` subqueries need not return a specific column — `SELECT o` is sufficient
* Generator fully validates entity names, properties, and parameter bindings inside the subquery at compile time

***

#### WITH Clause (Common Table Expressions)

`WITH` defines named CTEs whose results are referenced in the main SELECT, UPDATE, or JOIN. Each CTE body is a CPQL SELECT statement translated at compile time — the entire WITH + main query becomes a single SQL string literal in the generated repository method.

**Basic CTE:**
```csharp
[Query("WITH ActiveProducts AS (SELECT p FROM Product p WHERE p.IsActive = TRUE) " +
       "SELECT a FROM ActiveProducts a WHERE a.Price > :minPrice ORDER BY a.Name ASC")]
Task<IEnumerable<Product>> FindActiveAbovePriceAsync(decimal minPrice);
```

**Multiple CTEs:**
```csharp
[Query("WITH " +
       "  ActiveProducts AS (SELECT p FROM Product p WHERE p.IsActive = TRUE), " +
       "  Expensive AS (SELECT a FROM ActiveProducts a WHERE a.Price > :threshold) " +
       "SELECT e FROM Expensive e ORDER BY e.Price DESC")]
Task<IEnumerable<Product>> FindExpensiveActiveAsync(decimal threshold);
```

**CTE with aggregate — customer order summary:**
```csharp
[Query("WITH OrderCounts AS (" +
       "  SELECT o.CustomerId, COUNT(o) AS Total FROM Order o GROUP BY o.CustomerId" +
       ") " +
       "SELECT c FROM Customer c " +
       "JOIN OrderCounts oc ON oc.CustomerId = c.Id " +
       "WHERE oc.Total >= :minOrders")]
Task<IEnumerable<Customer>> FindActiveCustomersAsync(int minOrders);
```

**Recursive CTE — hierarchical data:**
```csharp
[Query("WITH RECURSIVE CategoryTree AS (" +
       "  SELECT c FROM Category c WHERE c.Id = :rootId " +
       "  UNION ALL " +
       "  SELECT child FROM Category child " +
       "  JOIN CategoryTree parent ON child.ParentId = parent.Id" +
       ") " +
       "SELECT t FROM CategoryTree t")]
Task<IEnumerable<Category>> FindCategorySubtreeAsync(int rootId);
```

**Rules:**

* CTE body is a CPQL SELECT — entity names and property names are validated at compile time
* CTE name is a plain identifier used only as an alias within the query; it is not an entity name and has no entity metadata
* When the main query or a subsequent CTE references a CTE by name in FROM, the generator treats its columns as the projected columns of the CTE body SELECT
* CTE JOIN uses explicit `ON condition` syntax (not relationship property path), since CTEs have no relationship metadata
* `RECURSIVE` keyword supported; the recursive member must be connected via `UNION ALL`; recursive bodies are passed through as-is to the provider (the generator does not validate recursive references)
* All `:paramName` references across WITH bodies and main query share the same namespace — must be unique and bound to method parameters
* Provider-specific emission: `WITH … AS (…)` is standard across SQL Server, PostgreSQL, and MySQL 8+

***

#### Intentionally Excluded from CPQL

These are excluded by design, not by technical limitation:

| Feature | Why excluded | Alternative |
|---------|--------------|-------------|
| UNION / INTERSECT / EXCEPT | JPQL does not support set operators; they are SQL-level, not entity-level operations | `[Query(NativeQuery = true)]` |
| Provider-specific functions not in scalar list | CPQL must be provider-agnostic; unmappable functions cannot be translated | `[Query(NativeQuery = true)]` |
| Nested subqueries (subquery inside subquery) | Deferred; covered by `NativeQuery = true` for now | `[Query(NativeQuery = true)]` |
| FETCH JOIN | Replaced by `Include` / `ThenInclude` on the query builder | `Include` / `ThenInclude` |

***

## Requirements

* All method name parsing must happen at compile time via the Roslyn source generator
* Invalid or unresolvable method names must produce a `Diagnostic` compile error
* Generated SQL must be a string literal in the emitted repository implementation
* Navigation property joins in derived query method names are limited to one level deep; deeper paths require `[Query]` with CPQL
* `In` / `NotIn` parameters must be emitted as provider-specific parameterized lists (not string concatenation)
* `Page<T>` return type must emit two SQL string literals: COUNT query + paginated SELECT
* `Slice<T>` return type must emit one SQL literal with `pageSize+1` rows (provider-specific template); no COUNT query; `HasNext` assigned from result count at runtime; compile-time SQL rule is not broken
* `Pageable` parameter must carry only `PageNumber` and `PageSize` — sort is forbidden inside `Pageable`
* `Sort` parameter must use a compile-time pre-generated lookup table; only `[Sortable]`-marked properties are valid sort columns; all other columns throw `InvalidSortException` at runtime — no dynamic SQL is produced
* `[NamedQuery]` SQL is embedded as a string literal at compile time — no runtime SQL string storage
* `[StoredProcedure]` call syntax must be provider-specific and generated as a string literal
* CPQL queries (`NativeQuery = false`) must be fully parsed and translated to SQL at compile time; invalid entity/property names produce `Diagnostic` errors
* Native SQL queries (`NativeQuery = true`) are embedded unchanged as string literals; generator does not validate content
* `CountDistinct` emits `SELECT COUNT(DISTINCT {primaryKey}) FROM … WHERE …` — it counts distinct entity instances, not distinct filter column values
* `PostLoad` lifecycle hook must fire after all derived SELECT methods that return entities
* `Delete` / `Remove` subject methods must invoke `PreRemove` / `PostRemove` lifecycle hooks

***

# 6. Relationships

## Features

### One-to-Many

* Parent holds `LazyCollection<TChild>`
* Annotated with `[OneToMany]`
* `mappedBy` specifies the FK property on the child side

***

### Many-to-One

* Child holds `LazyReference<TParent>`
* Annotated with `[ManyToOne]`
* FK column defined via `[JoinColumn]`

***

### One-to-One

* One side holds `LazyReference<T>`
* Annotated with `[OneToOne]`
* FK column defined via `[JoinColumn]`
* Owning side determined by `[JoinColumn]` presence
* `[OneToOne(MappedBy = "propertyName")]` for non-owning side

***

### Many-to-Many

* Both sides hold `LazyCollection<T>`
* Join table configured via `[JoinTable]`
* Annotated with `[ManyToMany]`

***

### Join Column

* `[JoinColumn("fk_column_name")]` specifies FK column name on the owning side
* Support `Nullable` on join column
* If omitted, default: `{RelatedEntityName}_id`

***

### Join Table

* `[JoinTable("join_table_name", JoinColumn = "left_fk", InverseJoinColumn = "right_fk")]`
* Required for `[ManyToMany]`
* Generator uses these names in all join table SQL

***

### Default Ordering

* `[OrderBy("column_name ASC")]` on a `LazyCollection` defines default sort order for that collection
* Generator includes ORDER BY in the collection load SQL

***

### Order Column

* `[OrderColumn(Name = "position")]` on a `[OneToMany]` or `[ManyToMany]` collection maintains a persistent integer position column in the child/join table
* Generator includes `ORDER BY position ASC` in the collection load SQL
* On insert, generator assigns position values (0-based) to each child before executing batch insert
* On delete of a child, generator updates position values of remaining children to close the gap
* `[OrderColumn]` and `[OrderBy]` are mutually exclusive on the same collection

***

### Fetch Type

* `FetchType.Lazy` (default for collections): load via `LazyCollection.GetAsync()`
* `FetchType.Eager` (default for single references in explicit include): generator emits JOIN in base SELECT SQL
* Fetch type set per relationship: `[OneToMany(Fetch = FetchType.Lazy)]`
* Eager fetch generates a JOIN query; result mapped via generated multi-column mapper

***

### Primary Key Join Column

* `[PrimaryKeyJoinColumn]` on a `[OneToOne]` child entity — the child's primary key **is** the foreign key to the parent; no separate FK column exists
* Used for true 1-to-1 shared-PK relationships (e.g., `User` and `UserProfile` share the same `id`)
* Owning side: child entity annotated with `[OneToOne]` + `[PrimaryKeyJoinColumn]`
* Generator emits `JOIN child ON child.id = parent.id` as a compile-time literal (no FK column reference)
* On INSERT of child: generator sets `child.Id = parent.Id` before executing INSERT — Id assignment is a runtime data operation, no dynamic SQL
* `[PrimaryKeyJoinColumn]` and `[JoinColumn]` are mutually exclusive on the same `[OneToOne]`

```csharp
[Entity] [Table("users")]
public class User {
    [Id] [GeneratedValue(GenerationType.Identity)] public int Id { get; set; }
    [OneToOne(MappedBy = "User")] public LazyReference<UserProfile> Profile { get; set; }
}

[Entity] [Table("user_profiles")]
public class UserProfile {
    [Id] [GeneratedValue(GenerationType.Assigned)] public int Id { get; set; }  // same as User.Id
    [OneToOne] [PrimaryKeyJoinColumn] public LazyReference<User> User { get; set; }
}
```

**Compile-time rule:** Join condition `ON child.id = parent.id` is a compile-time literal. Does not break any rule.

**Stateless rule:** Id assignment before INSERT is a data operation. Does not break any rule.

***

### Map-Keyed Relationships

* `[OneToMany]` or `[ManyToMany]` collection typed as `LazyMap<TKey, TValue>` — a dictionary-like lazy wrapper
* `[MapKey("column_name")]` on the property specifies which child column becomes the map key
* Generator emits the same `SELECT … WHERE parent_id = @id` SQL as a regular `[OneToMany]` — identical to `LazyCollection` load SQL
* After loading, result is grouped by the map key column into a `Dictionary<TKey, TValue>` — runtime data grouping, not SQL construction
* `GetAsync()` returns `IReadOnlyDictionary<TKey, TValue>`
* `TryGet()` / `Set(IDictionary<TKey, TValue>)` follow the same read-once pattern as `LazyCollection`

```csharp
[Entity]
public class Department {
    [OneToMany(MappedBy = "DepartmentId")]
    [MapKey("employee_code")]
    public LazyMap<string, Employee> EmployeesByCode { get; set; }
}
```

**Compile-time rule:** SELECT SQL is identical to `[OneToMany]` — compile-time literal. Key grouping is runtime LINQ, not SQL. Does not break any rule.

**Stateless rule:** Per-instance dictionary, same bounded exception as `LazyCollection`. Does not break any rule.

***

### Association Override
* Used when a `[MappedSuperclass]` defines a relationship and a subclass needs a different FK column name
* Used when an `[Embeddable]` contains a relationship and the owning entity needs to override the join column per embed site
* Multiple `[AssociationOverride]` attributes may appear on one class

```csharp
[Entity]
[AssociationOverride(Name = "Owner", JoinColumn = "admin_user_id")]
public class AdminDocument : BaseDocument { }
```

***

### Explicit Batch Relationship Loading

* Generator produces a `Load{Collection}ForManyAsync(IEnumerable<TParent>)` method per `[OneToMany]` / `[ManyToMany]` relationship
* Emits `SELECT … WHERE fk_id IN @parentIds` — one query for all parents, not one per parent
* After loading, calls `Set()` on each parent's lazy collection automatically
* Prevents N+1 without requiring a stateful session — developer calls it explicitly after loading the parent list

```csharp
// Generated method:
// SELECT * FROM order_items WHERE order_id IN @orderIds
await repo.LoadItemsForManyAsync(orders);
// Each order.Items.TryGet() now returns the loaded items
```

***

## Requirements

* Foreign key must be defined via `[JoinColumn]` or convention
* Relationship metadata generated at compile time
* No automatic graph synchronization
* Direction must be defined for cascade
* `[OneToOne]` owning side must have either `[JoinColumn]` or `[PrimaryKeyJoinColumn]` — not both
* `[PrimaryKeyJoinColumn]` child entity `[Id]` must be `GenerationType.Assigned` — generator emits `Diagnostic` error otherwise
* `[PrimaryKeyJoinColumn]` JOIN SQL emitted as `ON child.id = parent.id` compile-time literal — no runtime FK column lookup
* `[ManyToMany]` must have `[JoinTable]` on the owning side
* Eager fetch JOIN mapping code must be generated, not runtime-reflected
* `[OrderBy]` ORDER BY clause must be appended to generated collection load SQL
* `[MapKey]` column must exist on the child entity; `LazyMap<TKey, TValue>` generic type parameters must be compatible with the key column's .NET type — generator validates both at compile time
* `LazyMap` load SQL is identical to `[OneToMany]` SELECT literal — no new SQL construction; in-memory grouping only

***

# 7. Lazy Loading

## Features

### LazyCollection

* Loads collection explicitly via `GetAsync()`
* Default ordering from `[OrderBy]` if present
* `TryGet()` returns loaded data if already loaded, otherwise null (no DB call)
* `Set(IEnumerable<T> data)` injects pre-loaded data without DB call

***

### LazyMap

* Dictionary-keyed variant of `LazyCollection<T>` — used with `[MapKey]` relationships
* `GetAsync()` returns `IReadOnlyDictionary<TKey, TValue>`; keyed by the `[MapKey]` column value
* `TryGet()` returns the loaded dictionary or null — no DB call
* `Set(IDictionary<TKey, TValue> data)` injects pre-loaded data
* SQL emitted is identical to `[OneToMany]`; dictionary grouping happens in-memory after load — no dynamic SQL
* Read-once model: same per-instance cache rules as `LazyCollection`

***

### LazyReference

* Loads single entity explicitly via `GetAsync()`
* `TryGet()` returns loaded data if already loaded, otherwise null
* `Set(T entity)` injects pre-loaded data without DB call

***

## Behavior

* Data loaded only when `GetAsync()` is called
* Once loaded, cached in memory on the instance
* No automatic reload and no `Reload()` method — if fresh data is needed, call the repository to get a new entity instance
* `Set()` marks the reference as loaded; subsequent `GetAsync()` returns injected data without DB call

***

## Per-Instance Cache — Intentional Bounded Exception

`LazyCollection`, `LazyReference`, and `LazyMap` hold per-instance state (a loaded flag and cached data). This is an **intentional, bounded exception** to the stateless rule:

* It is **per-entity-instance state**, not a global identity map or cross-entity cache
* Repository methods are stateless — each call produces a new entity instance with an empty lazy load state
* Developers must treat entity instances as **read-once value objects** — do not reuse an instance across operations where freshness matters
* For fresh data: call the repository again and get a new entity instance; never attempt to refresh a lazy collection on an existing instance
* `Reload()` is explicitly **not provided** — its absence enforces the read-once model and prevents misuse patterns that treat entity instances as long-lived objects

***

## Requirements

* Must not access DB on property access
* Must support manual injection via `Set()` for testing and for `Include` result assembly
* `TryGet()` must not trigger DB call under any circumstances
* Must be thread-safe
* Must NOT implement `Reload()` — the correct pattern for fresh data is a new repository call
* `LazyMap.GetAsync()` returns `IReadOnlyDictionary<TKey, TValue>`; dictionary grouping is in-memory after SQL load — no dynamic SQL is produced
* `[MapKey]` column must exist on the child entity; generator validates at compile time and emits `Diagnostic` error if absent
* `Load{Map}ForManyAsync` batch loader follows same pattern as `Load{Collection}ForManyAsync` — executes one SQL literal; groups in-memory first by FK then by map key; calls `Set()` on each parent's `LazyMap`; no dynamic SQL

***

# 8. Execution Engine

## Requirements

* All queries executed through Dapper
* Use parameterized execution
* Async-only API
* No abstraction over Dapper
* Mapping must use strong typing

***

# 9. Cascade System

## Features

* `CascadeType.Persist` — cascade insert into children during `InsertGraphAsync`
* `CascadeType.Merge` — cascade update into children during `UpdateGraphAsync`
* `CascadeType.Remove` — cascade delete into children during `DeleteGraphAsync`
* `CascadeType.All` — shorthand for Persist + Merge + Remove

***

## Requirements

* Cascade must be opt-in via attribute: `[OneToMany(Cascade = CascadeType.All)]`
* No implicit cascade on basic CRUD operations (`InsertAsync`, `UpdateAsync`, `DeleteAsync`)
* Graph must be flattened before execution
* No recursive runtime traversal
* `CascadeType.Remove` on `[OneToMany]` must delete children before parent (topological order)
* `CascadeType.Persist` must assign FK on child after parent insert

***

# 10. Graph Handling

## Features

* Flat execution plan
* No recursion

***

## Requirements

* Build relationship graph at compile time
* Detect circular references
* Remove reverse relationships
* Convert graph to DAG
* Execute in topological order
* Circular reference detected → `Diagnostic` compile error

***

# 11. Batch Processing

## Features

* Batch insert
* Batch update
* Batch delete

***

## Requirements

* Must accept `IEnumerable<T>`
* Must flatten collections before execution
* Must not execute SQL per entity
* Must use `ExecuteAsync(collection)`

***

# 12. Batch Size Control

## Features

* Configurable batch size
* Automatic splitting into chunks

***

## Requirements

* Prevent exceeding DB limits
* Default batch size configurable via `DapperXOptions`
* Chunk processing must preserve order
* Must support override per operation

***

# 13. Bulk Insert Optimization

## Features

* SQL Server: `SqlBulkCopy`
* PostgreSQL: `COPY`
* MySQL: `LOAD DATA`
* SQLite: no native bulk mechanism — always falls back to regular batch INSERT via `ExecuteAsync(collection)`

***

## Requirements

* Must detect provider
* Generate provider-specific implementation (`SqlServerBulkExecutor`, `PostgreSqlBulkExecutor`, `MySqlBatchExecutor`; resolved at compile time per `DatabaseProvider`)
* Must fallback to normal batching if unsupported (SQLite, identity/sequence keys, secondary tables, converters, auditing, tenancy, element collections, or count below `BulkThreshold`)
* Must only be used for large datasets (threshold configurable via `IDapperXOptions.BulkThreshold`, default 5000)
* Must not be used for updates or deletes
* Bulk-eligible `InsertManyAsync` compares `list.Count` to `BulkThreshold`; at or above threshold uses native bulk executor; below uses chunked `ExecuteAsync(collection)` with `_options.BatchSize` (default 1000)
* SQLite: bulk optimization is permanently disabled; `BulkThreshold` is ignored for SQLite; no `Diagnostic` — silent fallback to batch

***

# 14. Multi-Database Support

## Features

* Multiple database providers

***

## Requirements

* SQL dialect differences must be handled:

| Feature | SQL Server | PostgreSQL | MySQL | SQLite |
|---|---|---|---|---|
| Paging | `OFFSET n ROWS FETCH NEXT m ROWS ONLY` | `LIMIT m OFFSET n` | `LIMIT m OFFSET n` | `LIMIT m OFFSET n` |
| Identity return | `OUTPUT INSERTED.Id` | `RETURNING id` | `SELECT LAST_INSERT_ID()` | `SELECT last_insert_rowid()` |
| Pessimistic lock | `WITH (UPDLOCK, ROWLOCK)` | `FOR UPDATE` | `FOR UPDATE` | Not supported → `Diagnostic` error |
| Lock timeout | `SET LOCK_TIMEOUT @timeout` | `SET lock_timeout = @timeout` | `NOWAIT` only | Not supported → `Diagnostic` error |
| Sequence | `NEXT VALUE FOR seq` | `nextval('seq')` | — | Not supported → `Diagnostic` error |
| Boolean literals | `1`/`0` | `TRUE`/`FALSE` | `1`/`0` | `1`/`0` |
| Regex | `REGEXP` not native → `Diagnostic` error | `~` operator | `REGEXP` | Requires extension → `Diagnostic` warning |
| Upsert | `MERGE INTO … USING … ON …` | `INSERT … ON CONFLICT DO UPDATE` | `INSERT … ON DUPLICATE KEY UPDATE` | `INSERT … ON CONFLICT DO UPDATE` (SQLite 3.24+; generator emits `ON CONFLICT`, not `INSERT OR REPLACE`) |
| Stored procedure call | `EXEC proc @p` | `SELECT * FROM proc(@p)` | `CALL proc(@p)` | Not supported → `Diagnostic` error |
| CURRENT_TIMESTAMP | `GETDATE()` | `CURRENT_TIMESTAMP` | `NOW()` | `datetime('now')` |
| CURRENT_DATE | `CAST(GETDATE() AS DATE)` | `CURRENT_DATE` | `CURDATE()` | `date('now')` |
| Table schema | `schema.table` | `schema.table` | — | Not supported → `Diagnostic` warning (Schema value ignored) |
| Multiple result sets | Supported | Supported | Supported | Not supported → `Diagnostic` error |
| Bulk insert | `SqlBulkCopy` | `COPY` | `LOAD DATA` | Falls back to batch (silent) |

* Separate implementation per provider
* No runtime branching logic for SQL
* Generator selects correct SQL per provider at compile time
* SQLite-specific Diagnostic errors emitted at compile time when unsupported features are detected: pessimistic locking, lock timeout, `GenerationType.Sequence`, stored procedures, multiple result sets
* SQLite minimum version requirements: upsert `ON CONFLICT DO UPDATE` requires SQLite 3.24+; `NULLS FIRST/LAST` requires SQLite 3.30.0+; recursive CTE requires SQLite 3.8.3+

### Verification (test matrix)

* Dialect differences in the table above must be verified by **compile-time** tests in **four** test assemblies — one per provider (`DapperXDatabaseProvider` MSBuild property): SqlServer ([`DapperX.Tests`](tests/DapperX.Tests/DapperX.Tests.csproj)), PostgreSql, MySql, Sqlite — not a single runtime-switching test suite
* Provider-sensitive generation tests must be linked from a shared source set and assert per-provider SQL literals (upsert, paging, identity return, bulk path, locking, auditing timestamps, etc.)
* SQLite unsupported features (pessimistic lock, sequences, stored procedures, multiple result sets, schema attribute, bulk API) must have **compile-time** Diagnostic tests in the Sqlite test assembly only
* Provider-agnostic tests (CPQL parser/validator, metadata Diagnostics, builder/translator Theory over provider string) may run once in the SqlServer test assembly
* **Integration** tests must run against a real database per provider using Testcontainers (or equivalent); one integration test assembly per provider; Docker required in CI
* Integration tasks must not be marked complete until both the compile-time matrix test and the Testcontainers test exist for that feature (see `Tasks.md` EPIC 26a)
* Planning layout: solution and test tree in `Structures.md` §1 and §2.11; task breakdown and coverage legend in `Tasks.md` (preamble + EPIC 26a)

***

# 15. Lifecycle Events

## Features

### Entity Lifecycle

* `[PrePersist]` — invoked before insert
* `[PostPersist]` — invoked after insert
* `[PreUpdate]` — invoked before update
* `[PostUpdate]` — invoked after update
* `[PreRemove]` — invoked before delete
* `[PostRemove]` — invoked after delete
* `[PostLoad]` — invoked after entity is loaded from DB

***

### Batch Lifecycle

* `[PrePersistBatch]` — invoked before batch insert
* `[PostPersistBatch]` — invoked after batch insert
* `[PreUpdateBatch]` — invoked before batch update
* `[PostUpdateBatch]` — invoked after batch update
* `[PreRemoveBatch]` — invoked before batch delete
* `[PostRemoveBatch]` — invoked after batch delete

***

### Entity Listeners

* `[EntityListeners(typeof(AuditListener))]` on an entity class registers an external listener class
* Listener class implements lifecycle methods annotated with the same lifecycle attributes
* Generator wires listener invocations into generated repository code alongside entity-level hooks
* Listener may be shared across multiple entities

***

## Requirements

* Must be invoked via generated code
* No reflection allowed
* Must preserve execution order:
  * Batch hook wraps entity hooks: `PrePersistBatch` → (per-entity `PrePersist` → insert → `PostPersist`) → `PostPersistBatch`
  * Same pattern for update and delete batches
* Listener invocations must fire after entity-level hooks in the same position
* `[PostLoad]` must fire after every SELECT that returns entities (CRUD, derived query methods, graph loads)

***

# 16. Concurrency Control

## Features

* Version-based optimistic concurrency (default)
* Pessimistic locking via `WithLock(LockMode.Pessimistic)` in queries

***

## Requirements

### Optimistic

* UPDATE must include `WHERE Version = @currentVersion`
* DELETE must include `WHERE Version = @currentVersion`
* Version must increment automatically in generated UPDATE SQL
* If `affected rows = 0` → throw `ConcurrencyException`

### Batch Concurrency Behavior

* After batch execution, if total affected rows < batch size → throw `ConcurrencyException` listing all conflicting entity keys
* Partial success is not treated as success — throw on any conflict
* In graph operations, conflict on any entity rolls back the entire transaction

### Pessimistic

* Query with `WithLock(LockMode.Pessimistic)` generates provider-specific lock hint
* Lock is held for the duration of the transaction
* Generator emits a warning diagnostic if pessimistic lock is used without a detectable transaction context

***

# 17. Transactions

## Features

* Manual transaction via `IDbTransaction` injection
* Optional generated transactional wrapper method

***

## Requirements

* Graph operations must run in transaction
* Must support `IDbTransaction` injection on all repository methods
* Must rollback on failure
* Generated transactional wrapper: each entity gets a `WithTransactionAsync(Func<Task>)` helper that wraps a unit of work in a new transaction using the injected `IDbConnection`

***

# 18. Many-to-Many

## Features

* Join table handling via `[JoinTable]`

***

## Requirements

* `[JoinTable]` must define: table name, owning FK column, inverse FK column — generator emits `Diagnostic` errors DPX076–DPX078 when any part is missing
* Generate SQL for join table: INSERT join records, DELETE join records
* Batch insert/delete join records
* `UpdateGraphAsync` with `CascadeType.Merge` reconciles join rows for loaded collections: delete all links for the parent, then batch re-insert from the collection (join table only — no child entity CRUD)
* Do not cascade into related entity tables — only manage the join table relationship
* Generator emits join table SQL as literals (no runtime assembly)

***

# 19. Debugging and Observability

## Features

### SQL Logging

* Log the SQL string (with `@param` placeholders) before each execution
* Configurable via `DapperXOptions.LogSql` flag (default: false)

***

### Parameter Logging

* Log parameter names and their runtime values alongside the SQL
* Configurable via `DapperXOptions.LogParameters` flag (default: false)
* **Security warning:** parameter values may contain PII, passwords, or sensitive data — must only be enabled in development/debug environments

***

### Executable SQL Output

* Format and log the SQL with parameter values substituted inline — produces a copy-paste-ready statement for direct execution in a database tool
* Configurable via `DapperXOptions.LogExecutableSql` flag (default: false)
* **Security warning:** must never be enabled in production — exposes all parameter values in plain text
* Value formatting rules per type:
  * `string` / `char` → `'value'` (single-quoted; internal single quotes escaped as `''`)
  * `int` / `long` / `short` → `123` (unquoted)
  * `decimal` / `double` / `float` → `45.67` (unquoted, culture-invariant)
  * `bool` → dialect-aware (`1`/`0` or `TRUE`/`FALSE`)
  * `DateTime` / `DateTimeOffset` → `'2024-01-01 12:00:00'`
  * `Guid` → `'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'`
  * `null` → `NULL`
  * `IEnumerable<T>` (IN parameter) → `('a', 'b', 'c')` (each element formatted by its type)

***

### Structured Log Entry

The logger receives a `DapperXLogEntry` object — not a plain string — so callers can choose what to output:

```csharp
public class DapperXLogEntry
{
    public string MethodName { get; }       // repository method that triggered execution
    public string Sql { get; }             // SQL with @param placeholders (always populated)
    public IReadOnlyDictionary<string, object> Parameters { get; }  // null unless LogParameters = true
    public string ExecutableSql { get; }   // null unless LogExecutableSql = true
    public DateTime Timestamp { get; }
}
```

***

## Requirements

* SQL must be readable in generated code as string literals
* Logger is configured via `DapperXOptions.Logger` (`Action<DapperXLogEntry>`)
* Logger is invoked **before** execution — not after — so slow/failed queries are still logged
* `LogSql` must be enabled for any logging to occur; `LogParameters` and `LogExecutableSql` are ignored if `LogSql = false`
* `LogParameters` and `LogExecutableSql` are independent flags — either or both may be enabled
* `ExecutableSql` substitution must be purely for output — never used to execute SQL; all actual execution uses parameterized Dapper calls
* `ExecutableSql` value formatting must be culture-invariant and dialect-aware for booleans
* `MethodName` in `DapperXLogEntry` is the repository method name resolved at compile time — not a runtime stack trace lookup
* No hidden execution — all SQL paths invoke the logger if logging is enabled
* `LogParameters = true` and `LogExecutableSql = true` must emit a `Diagnostic` warning at compile time if `DatabaseProvider` is not a development-only provider (future: tie to build configuration)

***

# 20. Configuration

## Features

* Batch size
* Bulk enable flag
* Provider selection
* SQL logging (with structured log entry)
* Parameter value logging (opt-in)
* Executable SQL output (opt-in, development only)

***

## Requirements

* `DapperXOptions` holds runtime configuration:

| Property | Type | Default | Description |
|---|---|---|---|
| `BatchSize` | `int` | `1000` | Default batch chunk size |
| `BulkThreshold` | `int` | `5000` | Row count above which bulk insert is used |
| `Logger` | `Action<DapperXLogEntry>` | `null` | Structured log entry handler |
| `LogSql` | `bool` | `false` | Enable SQL logging (master switch) |
| `LogParameters` | `bool` | `false` | Include parameter name+value in log entry |
| `LogExecutableSql` | `bool` | `false` | Include inline-substituted executable SQL in log entry |

* `DatabaseProvider` enum is a **compile-time constant** — set at generator level (e.g., via MSBuild property or assembly-level attribute); generator selects provider-specific SQL based on this value
* Allow override of `BatchSize` and `BulkThreshold` per operation via optional method parameters on batch methods (`batchSize`, `bulkThreshold` on `InsertManyAsync`; `batchSize` on `UpdateManyAsync`, `DeleteManyAsync`, and `UpsertManyAsync`) — override wins over `DapperXOptions` when supplied
* All logging options are purely runtime — they do not affect generated SQL
* `LogParameters` and `LogExecutableSql` are silently ignored if `LogSql = false` or `Logger = null`

***

# 21. Error Handling

## Features

* Defined exception types

***

## Requirements

* Throw `ConcurrencyException` on optimistic conflict (include list of conflicting keys for batch)
* Throw `MappingException` at compile time (via `Diagnostic`) for invalid config
* Throw `SqlExecutionException` (wraps provider exception, includes failing SQL via `Sql` property) for execution failure — via `DbExecutor` at runtime
* Must fail fast — no silent fallback

***

# 22. Excluded Features

## Must not implement

* Identity map
* Change tracking
* Persistence context (`flush`, `clear`, `detach`, `merge` semantics)
* Proxy-based lazy loading
* Automatic bidirectional sync
* `refresh()` operation
* DDL schema generation
* Runtime metadata scanning
* `@Inheritance` / discriminator-column table-per-hierarchy mapping (future)

***

# 23. Code Generation System

## Requirements

* Must use Roslyn incremental source generator
* Must process at compile time
* Must generate:
  * Sealed `{Name}RepositoryImpl` implementation class per `[Repository]`-annotated interface (strips leading `I`, appends `Impl`); SELECT-only for `[Immutable]` entities
  * Inline SQL string literals per operation — all table/column names, JOIN conditions, and WHERE clauses baked in at compile time
  * `GetAllAsync()` / `GetAllAsync(Sort)` / `GetAllAsync(Pageable)` / `GetAllAsync(Sort, Pageable)` overloads
  * `DeleteAllByIdAsync` — `DELETE WHERE id IN @ids` literal; single-key entities only
  * Derived query method implementations from method names including `IncludeDeleted` parameter
  * Paired soft-delete SQL literals per SELECT method (WITH filter / WITHOUT filter) for `IncludeDeleted` support
  * Sort lookup tables (one ORDER BY fragment literal per `[Sortable]` property × direction)
  * `ResolveColumn(propertyName)` per-entity switch — maps property names to column names without reflection
  * `FILTER_{NAME}` compile-time string constants per `[GlobalFilter]`; conditional append in all SELECT/UPDATE/DELETE methods
  * Execution plans for graph operations
  * Lifecycle invocations (entity + batch + listener + auditing timing)
  * Auditing field injection (`CURRENT_TIMESTAMP` / `GetCurrentUser()` calls) in INSERT/UPDATE SQL
  * Soft-delete DELETE→UPDATE rewrite; `is_deleted = 0` appended to all SELECTs
  * Tenancy filter injection; tenant value SET in INSERT
  * Eager-fetch JOIN mapping code
  * Projection query column lists
  * Formula expressions in SELECT column list
  * `[ColumnTransformer]` Read/Write SQL expressions in SELECT/INSERT/UPDATE
  * `[Generated]` column exclusion from INSERT/UPDATE; re-SELECT SQL after mutation (per provider)
  * Join table SQL for many-to-many
  * Secondary table LEFT JOINs in SELECT; split INSERT/UPDATE/DELETE per table in topological order
  * `[PrimaryKeyJoinColumn]` `ON child.id = parent.id` JOIN condition; child Id pre-assignment before INSERT
  * `LazyMap<K,V>` load SQL (identical to `[OneToMany]` SELECT); in-memory grouping by `[MapKey]` column
  * `Load{Collection}ForManyAsync` and `Load{Map}ForManyAsync` batch relationship loaders
  * Element collection load/insert/delete SQL per `[ElementCollection]`
  * Named entity graph SQL switch (one literal per graph, all filters applied)
  * Upsert SQL per provider (`MERGE` / `ON CONFLICT DO UPDATE` / `ON DUPLICATE KEY UPDATE`)
  * Stored procedure call SQL per provider; OUT parameter capture; multiple result set handling
  * CPQL query translation — entity/property names → table/column names; path expressions → JOINs; window functions; all 23 scalar functions; CTEs; subqueries; CASE/WHEN; arithmetic
  * `MethodName` compile-time string literal in every generated method (for `DapperXLogEntry`)
  * `GetAllSliceAsync(Pageable)` and `GetAllSliceAsync(Sort, Pageable)` — provider-specific `pageSize+1` paging template; no COUNT query; `PostLoad` per result; all filters applied
  * SQLite-specific `Diagnostic` compile errors for unsupported features (pessimistic lock, sequences, stored procedures, multiple result sets)
  * `[Index]` attribute stored as `IndexMetadata` on entity model — **no SQL or DDL emitted** under any circumstance

***

## Constraints

* No runtime metadata scanning
* No reflection in generated or runtime code — `ResolveColumn()` switch replaces all runtime property introspection
* Must validate all mapping at compile time via `Diagnostic` errors
* Must support incremental generation (re-generate only changed entities)
* All SQL must be a compile-time string literal or a pre-generated literal selected from a compile-time switch at runtime — no dynamic SQL from user-controlled input

***

# 24. Runtime Usage

## Requirements

* Developer defines entities with JPA-like attributes
* Developer declares derived query methods on repository interfaces
* Generator produces full repository implementation
* Developer uses repository methods only
* Must support async execution
* Must expose query API and graph operations

***

# 25. Execution Flow

## InsertGraph Example

1. Invoke `PrePersistBatch` (and listener equivalent)
2. Invoke `PrePersist` per entity (and listener equivalent)
3. Insert root entities (batch/bulk)
4. Assign generated keys back to root entity instances
5. Flatten children, assign FK values from parent keys
6. Insert children (batch) — repeat for each graph level
7. Insert join table records for many-to-many relationships
8. Invoke `PostPersist` per entity
9. Invoke `PostPersistBatch`

***

## Requirements

* Must be deterministic
* Must avoid recursion
* Must handle batching at each level
* Generated key must be assigned back before FK assignment to children

***

# 26. Performance Requirements

* Must minimize DB round trips — batch operations use `ExecuteAsync(collection)` (never per-entity loops); SQL call count must be O(1) not O(n) for `InsertManyAsync`, `UpdateManyAsync`, `DeleteManyAsync`
* Must operate on sets, not individual rows
* Must minimize memory allocations — `GlobalFilter` conditional append has zero allocation overhead when no filter is active; `ResolveColumn()` switch must be O(1) lookup; no repeated `IEnumerable<T>` enumeration in any batch path
* Must avoid overhead from tracking — no change tracking, no identity map, no proxy overhead
* `Load{Collection}ForManyAsync` and `Load{Map}ForManyAsync` must issue exactly one SQL call regardless of parent count — N=1 queries not N=n
* `LazyCollection`, `LazyReference`, and `LazyMap` per-instance caches must prevent repeated DB calls within the same entity instance lifecycle
* CPQL translation must produce zero runtime overhead per call — all translation happened at compile time; runtime executes pre-built SQL string literals only
* `Slice<T>` saves one DB round trip per page compared to `Page<T>` — the COUNT query is eliminated; useful for large datasets where total count is not needed

***

# 27. Embeddable Types

## Features

* `[Embeddable]` marks a class as a value object that maps to columns in the owning entity's table
* `[Embedded]` on a property embeds the `[Embeddable]` class's columns into the owning entity
* `[AttributeOverride(Property = "Street", Column = "billing_street")]` overrides column names per embed site

***

## Example

```
[Embeddable]
public class Address {
    [Column] public string Street { get; set; }
    [Column] public string City { get; set; }
}

[Entity]
public class User {
    [Embedded]
    public Address BillingAddress { get; set; }   // maps to: street, city

    [Embedded]
    [AttributeOverride(Property = "Street", Column = "shipping_street")]
    [AttributeOverride(Property = "City",   Column = "shipping_city")]
    public Address ShippingAddress { get; set; }  // maps to: shipping_street, shipping_city
}
```

***

## Requirements

* Generator must flatten embedded properties into the owning entity's column list
* `[AttributeOverride]` must override column names per embed site
* Embeddable classes must not have their own `[Id]` or `[Table]`
* Embedded properties must be included in INSERT, UPDATE, and SELECT SQL of the owning entity
* `[Embedded]` property typed as null means all embedded columns are null

***

# 28. Type Converters

## Features

* `[Converter(typeof(MyConverter))]` on a property applies a custom converter for read/write
* Converter handles mapping between the .NET property type and the DB column type
* Built-in converters:
  * `EnumToStringConverter<TEnum>` — stores enum as string
  * `EnumToIntConverter<TEnum>` — stores enum as int
  * `JsonConverter<T>` — stores object as JSON string
  * `UtcDateTimeConverter` — normalizes DateTime to UTC
* `[Enumerated(EnumType.String)]` — shorthand for `[Converter(typeof(EnumToStringConverter<TEnum>))]`; avoids specifying the generic converter type explicitly
* `[Enumerated(EnumType.Ordinal)]` — shorthand for `[Converter(typeof(EnumToIntConverter<TEnum>))]`

***

## Requirements

* Converter class must implement `IValueConverter<TProperty, TColumn>`
* Generator emits converter call in both read path (after Dapper maps column) and write path (before parameter binding)
* Converter instantiation is compile-time (no runtime `new` via reflection)
* Converter must be stateless
* If converter throws, exception propagates as `SqlExecutionException`

***

# 29. Entity Listeners

## Features

* External listener class registered on an entity via `[EntityListeners(typeof(AuditListener))]`
* Listener methods annotated with same lifecycle attributes (`[PrePersist]`, `[PostLoad]`, etc.)
* One listener class may be registered on multiple entities

***

## Requirements

* Generator detects `[EntityListeners]` on entity and emits listener invocations in repository code
* Listener must receive the entity instance as a parameter
* Listener invocations fire at the same lifecycle position as entity-level hooks
* Listener class must have a no-arg constructor (generator emits `new AuditListener()` inline, no DI)
* If DI is needed for the listener, developer must use a static service locator pattern; generator does not resolve from DI container

***

# 30. Locking Modes

## Features

* `LockMode.Optimistic` — default; version-based, no SQL lock hint
* `LockMode.Pessimistic` — exclusive write lock; prevents concurrent reads and writes (`FOR UPDATE` / `WITH (UPDLOCK, ROWLOCK)`)
* `LockMode.PessimisticRead` — shared read lock; allows concurrent reads but prevents writes (`FOR SHARE` / `WITH (HOLDLOCK, ROWLOCK)`); maps to JPA's `PESSIMISTIC_READ`

***

## Provider-Specific Lock SQL

| Lock mode | SQL Server | PostgreSQL | MySQL | SQLite |
|---|---|---|---|---|
| `LockMode.Pessimistic` | `WITH (UPDLOCK, ROWLOCK)` | `FOR UPDATE` | `FOR UPDATE` | Not supported → `Diagnostic` error |
| `LockMode.PessimisticRead` | `WITH (HOLDLOCK, ROWLOCK)` | `FOR SHARE` | `FOR SHARE` (8.0+) / `LOCK IN SHARE MODE` | Not supported → `Diagnostic` error |

**Compile-time rule:** All lock SQL fragments are compile-time string literals selected by provider — no dynamic SQL. Does not break any rule.

**Stateless rule:** Lock hints are stateless per-query directives. Does not break any rule.

***

## Requirements

* Lock mode is set per query via `WithLock(LockMode, TimeoutMs = 0)` or `LockMode` parameter in derived methods
* Both `LockMode.Pessimistic` and `LockMode.PessimisticRead` must be used inside a transaction; a `Diagnostic` warning is emitted if no transaction is detectable at compile time
* Generated lock SQL must be a string literal selected by provider at compile time
* Lock applies to the root entity only; included related entities are not locked unless separately queried with lock
* SQLite: both `Pessimistic` and `PessimisticRead` → `Diagnostic` compile error (file-level locking only)

### Lock Timeout

* `WithLock(LockMode.Pessimistic, TimeoutMs = 5000)` — wait up to N ms for the lock; fail if not acquired
* `WithLock(LockMode.Pessimistic, TimeoutMs = 0)` — `NOWAIT` / fail immediately if locked
* `TimeoutMs` is a compile-time constant in the method call; generator emits the appropriate SQL fragment per provider

| Provider | Timeout = 0 | Timeout = N ms |
|---|---|---|
| SQL Server | `WITH (UPDLOCK, ROWLOCK)` + `SET LOCK_TIMEOUT 0` | `SET LOCK_TIMEOUT @timeout` + `WITH (UPDLOCK, ROWLOCK)` |
| PostgreSQL | `FOR UPDATE NOWAIT` | `SET lock_timeout = @timeout` + `FOR UPDATE` |
| MySQL | `FOR UPDATE NOWAIT` (8.0+) | `FOR UPDATE NOWAIT` + `Diagnostic` warning (no per-query timeout; non-zero timeout treated as NOWAIT) |
| SQLite | `Diagnostic` error — pessimistic locking not supported | `Diagnostic` error — pessimistic locking not supported |

* `@timeout` is always an **integer (milliseconds)** — no string concatenation, no `ms` suffix in the SQL template
* PostgreSQL accepts integer milliseconds directly: `SET lock_timeout = 5000` sets a 5-second timeout
* `SET LOCK_TIMEOUT` / `SET lock_timeout` is a parameterized Dapper call emitted as a **separate statement before the SELECT** — SQL structure is a compile-time template, only the integer value is a runtime parameter
* MySQL limitation: only `NOWAIT` is supported; any non-zero `TimeoutMs` value → generator emits `Diagnostic` warning and falls back to `NOWAIT`
* SQLite limitation: file-level locking only; no row-level or table-level lock hints; any `WithLock(LockMode.Pessimistic)` usage with SQLite provider → `Diagnostic` compile error

***

# 31. Auditing

## Features

* `[CreatedDate]` — auto-set to `CURRENT_TIMESTAMP` on INSERT; excluded from UPDATE
* `[LastModifiedDate]` — auto-set to `CURRENT_TIMESTAMP` on both INSERT and UPDATE
* `[CreatedBy]` — auto-set to `IAuditingProvider.GetCurrentUser()` on INSERT; excluded from UPDATE
* `[LastModifiedBy]` — auto-set to `IAuditingProvider.GetCurrentUser()` on INSERT and UPDATE
* `IAuditingProvider` — interface injected into repository; provides current user identity at runtime

***

## Requirements

* `[CreatedDate]` and `[CreatedBy]` must be treated as `Updatable = false` automatically — generator never includes them in UPDATE SQL
* `[LastModifiedDate]` and `[LastModifiedBy]` must be included in both INSERT and UPDATE SQL
* Generator emits `CURRENT_TIMESTAMP` literal (dialect-specific) for date fields — no runtime clock call
* Generator emits `_auditingProvider.GetCurrentUser()` call for `[CreatedBy]` / `[LastModifiedBy]` — `IAuditingProvider` injected into repository constructor
* Auditing fields must fire before lifecycle hooks (`PrePersist`, `PreUpdate`) so hooks see the populated values
* `[CreatedDate]` / `[CreatedBy]` are valid on `[MappedSuperclass]` and inherited by all subclasses
* If no `IAuditingProvider` is registered and `[CreatedBy]` / `[LastModifiedBy]` are present → `Diagnostic` warning at compile time

***

# 32. Soft Delete

## Features

* `[SoftDelete]` on an entity class redirects all DELETE operations to an UPDATE that sets a flag
* Configurable with `Column` (flag column name, default `is_deleted`) and `DeletedAtColumn` (timestamp column, optional)
* All SELECT queries for the entity automatically append `WHERE is_deleted = 0` (or equivalent)
* Hard DELETE methods are not generated; a `HardDeleteAsync` method is generated separately for privileged use

***

## Requirements

* Generator rewrites `DeleteAsync`, `DeleteByIdAsync`, `DeleteManyAsync`, `DeleteGraphAsync` to emit `UPDATE … SET is_deleted = 1 [, deleted_at = CURRENT_TIMESTAMP] WHERE id = @id [AND version = @version]`
* All SELECT queries (CRUD, derived methods, CPQL) automatically include `AND is_deleted = 0` in WHERE clause
* `[Version]` check is still applied to soft-delete UPDATE — concurrency safety preserved
* `PreRemove` / `PostRemove` lifecycle hooks still fire for soft deletes
* `HardDeleteAsync` is generated as a separate method that emits a true `DELETE` statement — for admin/cleanup use
* CPQL `DELETE FROM …` statements on a soft-delete entity emit the UPDATE form automatically; use `[Query(NativeQuery = true)]` to bypass
* `[SoftDelete]` on a `[MappedSuperclass]` applies to all subclasses

***

# 33. Multi-Tenancy

## Features

* `[TenantId]` on a property marks it as the tenant discriminator column
* Generator appends `AND tenant_id = @tenantId` to all SELECT, UPDATE, DELETE SQL for the entity automatically
* `ITenantProvider` interface supplies the current tenant ID at runtime — injected into repository constructor

***

## Requirements

* All SELECT queries (CRUD, derived methods, CPQL, graph loads) automatically include `AND {tenant_column} = @tenantId`
* All UPDATE and DELETE SQL automatically include `AND {tenant_column} = @tenantId` in WHERE clause — prevents cross-tenant mutation
* INSERT SQL automatically sets the tenant column value from `ITenantProvider.GetCurrentTenantId()`
* `ITenantProvider` is injected into repository constructor; missing registration → `Diagnostic` compile warning if `[TenantId]` is present
* CPQL queries on a tenant-isolated entity automatically receive the tenant filter appended — cannot be bypassed via CPQL; use `[Query(NativeQuery = true)]` to bypass
* `[TenantId]` property is treated as `Updatable = false` automatically — tenant column cannot be changed after insert
* `[TenantId]` is valid on `[MappedSuperclass]` and inherited by all subclasses

***

# 34. Element Collection

## Features

* `[ElementCollection]` on a `LazyCollection<T>` property stores a collection of **primitives or embeddables** in a separate child table — no entity class, no `[Id]`, values owned entirely by the parent
* `[CollectionTable("table_name", JoinColumn = "fk_column")]` specifies the child table name and FK column; required when `[ElementCollection]` is present
* `[Column("value_column")]` on the property specifies the value column name (for primitive collections)
* For `[Embeddable]` element types, each embeddable property maps to a column in the collection table using the same `[AttributeOverride]` mechanism

```csharp
[Entity]
public class Product {
    [ElementCollection]
    [CollectionTable("product_tags", JoinColumn = "product_id")]
    [Column("tag")]
    public LazyCollection<string> Tags { get; set; }

    [ElementCollection]
    [CollectionTable("product_images", JoinColumn = "product_id")]
    [OrderColumn(Name = "position")]
    public LazyCollection<ProductImage> Images { get; set; }  // [Embeddable]
}
```

***

## Requirements

* Generator emits `SELECT … FROM collection_table WHERE fk_column = @parentId` for `GetAsync()`
* Generator emits batch `INSERT INTO collection_table (fk_column, value_column) VALUES …` for insert
* Generator emits `DELETE FROM collection_table WHERE fk_column = @parentId` before re-inserting on update
* Parent `InsertAsync` / `UpdateAsync` / `DeleteAsync` persist element collections only when the `LazyCollection` is loaded (`IsLoaded`); unloaded collections are left unchanged on update
* `[OrderColumn]` is supported on `[ElementCollection]` — generator manages position column
* `[AttributeOverride]` supported for embeddable element types per collection table site
* `[ElementCollection]` collections are owned by the parent — no independent lifecycle or repository
* `[CollectionTable]` must define table name and join column; generator emits `Diagnostic` error if absent

***

# 35. Composite Keys

## Features

### @IdClass

```csharp
[Entity]
[IdClass(typeof(OrderItemId))]
public class OrderItem {
    [Id] public int OrderId { get; set; }
    [Id] public int ProductId { get; set; }
}

public class OrderItemId {
    public int OrderId { get; set; }
    public int ProductId { get; set; }
}
```

### @EmbeddedId

```csharp
[Entity]
public class OrderItem {
    [EmbeddedId]
    public OrderItemId Id { get; set; }  // must be [Embeddable]
}

[Embeddable]
public class OrderItemId {
    public int OrderId { get; set; }
    public int ProductId { get; set; }
}
```

***

## Requirements

* Generator detects `[IdClass]` or `[EmbeddedId]` and builds composite key structure at compile time
* Generated `GetByIdAsync` and `DeleteByIdAsync` accept the composite key type; `WHERE` clause includes all key columns: `WHERE key1 = @key1 AND key2 = @key2` — both are compile-time string literals
* `FindAllByIdAsync` is **not generated** for composite-key entities — a collection of composite keys would require dynamic SQL (N OR clauses) that cannot be pre-generated as a compile-time literal; generator emits `Diagnostic` error if declared on a composite-key entity's repository interface
* Developers needing multi-row lookup on composite-key entities must use derived query methods with IN on individual key columns (e.g., `FindByOrderIdInAsync`) or CPQL
* `[GeneratedValue]` is not supported on composite keys — `GenerationType.Assigned` only
* Composite FK relationships referencing a composite-key entity use `[JoinColumn]` per key column
* `[IdClass]` key properties must match entity `[Id]` property names exactly; generator validates at compile time
* `[EmbeddedId]` class must be `[Embeddable]`; must not have `[Id]` on individual properties

***

# 36. Named Entity Graph

## Features

* `[NamedEntityGraph(Name, AttributeNodes = new[] { "relationshipProp" })]` on an entity class defines a reusable fetch plan
* `SubGraphs = new[] { "collectionRelationshipProp" }` on `[NamedEntityGraph]` joins that collection; nested nodes use `[SubGraph("collectionProp", GraphName = "graphName", AttributeNodes = new[] { "nestedProp" })]` on the entity class (valid C# attribute parameter types)
* Multiple `[NamedEntityGraph]` attributes allowed per entity
* Applied in derived query methods or `[Query]` methods via the `EntityGraph` parameter:

```csharp
[Entity]
[NamedEntityGraph("order.withItems",
    AttributeNodes = new[] { "Items", "Customer" })]
[NamedEntityGraph("order.withItemsAndProduct",
    AttributeNodes = new[] { "Items", "Customer" },
    SubGraphs = new[] { "Items" })]
[SubGraph("Items", GraphName = "order.withItemsAndProduct", AttributeNodes = new[] { "Product" })]
public class Order { … }

// Repository interface:
Task<IEnumerable<Order>> FindByStatusAsync(string status, string EntityGraph = null);
```

***

## Requirements

* Generator pre-generates one SQL literal per named entity graph (each with the appropriate JOINs baked in)
* Runtime selects the correct SQL via a compile-time switch on the `EntityGraph` string — same pattern as Sort lookup table
* `EntityGraph = null` selects the default (no-join) SQL
* Invalid graph name at runtime → `InvalidEntityGraphException`
* Generator validates `AttributeNodes` and `SubGraph` relationship names at compile time
* Named entity graph fetch plan entirely replaces `Include`/`ThenInclude` for that call — they must not be combined
* Each pre-generated named entity graph SQL literal must pass through the same `SoftDeleteGenerator` and `TenancyGenerator` pipeline as all other SELECT methods — soft-delete filters (`AND is_deleted = 0`) and tenancy filters (`AND tenant_id = @tenantId`) must be baked into every graph variant's SQL literal; a graph query that bypasses these filters is a security and correctness violation

***

# 37. Column Transformer

## Features

* `[ColumnTransformer(Read = "sql_expr", Write = "sql_expr")]` applies SQL-level transformations to a column on read and write
* `Read` expression is included verbatim in the SELECT column list (replaces the bare column name)
* `Write` expression is included in INSERT/UPDATE parameter binding; `?` is the placeholder for the value
* Used for database-level encrypt/decrypt, compression, encoding, etc.
* Different from `[Converter]` (which is C#-level) — transformation happens inside the database

```csharp
// Database-level encryption:
[ColumnTransformer(
    Read  = "pgp_sym_decrypt(ssn, 'secret_key')",
    Write = "pgp_sym_encrypt(?, 'secret_key')")]
public string Ssn { get; set; }

// Stored as uppercase:
[ColumnTransformer(
    Read  = "UPPER(country_code)",
    Write = "UPPER(?)")]
public string CountryCode { get; set; }
```

***

## Requirements

* `Read` expression is embedded verbatim in the SELECT column list — generator wraps the column name with the expression at compile time
* `Write` expression replaces the raw parameter in INSERT/UPDATE; `?` is substituted with the Dapper `@paramName` placeholder
* `[ColumnTransformer]` properties are included in INSERT, UPDATE, and SELECT SQL (unlike `[Formula]` which is SELECT-only)
* `[ColumnTransformer(Read)]` only (no Write) — property is mapped on read but excluded from INSERT/UPDATE (effectively read-only with transformation)
* `[ColumnTransformer(Write)]` only (no Read) — raw column value returned on read, transformed on write
* Both `Read` and `Write` are native SQL strings (not CPQL) — validated for non-empty at compile time but not parsed
* `[ColumnTransformer]` and `[Converter]` must not be combined on the same property — generator emits `Diagnostic` error

***

# 38. Generated Columns

## Features

* `[Generated(GenerationTime.Insert)]` — column value is set by the database on INSERT (trigger, default expression, computed column); excluded from INSERT SQL; value re-fetched immediately after INSERT
* `[Generated(GenerationTime.Always)]` — column value is set by the database on both INSERT and UPDATE; excluded from both INSERT and UPDATE SQL; value re-fetched after each mutation
* Different from `[Formula]` (which is a SELECT expression evaluated per query) and `[Column(Insertable=false)]` (which just skips the column without re-fetching)

```csharp
[Entity]
public class Order {
    [Id] [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Generated(GenerationTime.Insert)]
    public DateTime CreatedAt { get; set; }     // set by DB DEFAULT CURRENT_TIMESTAMP on INSERT

    [Generated(GenerationTime.Always)]
    public decimal TotalWithTax { get; set; }   // computed column updated by DB trigger
}
```

***

## Requirements

* `[Generated(GenerationTime.Insert)]` properties must be excluded from INSERT SQL; excluded from UPDATE SQL
* `[Generated(GenerationTime.Always)]` properties must be excluded from both INSERT and UPDATE SQL
* After INSERT: generator emits a re-SELECT for all `[Generated]` properties using the generated key: `SELECT generated_col FROM table WHERE id = @id`; provider alternatives: `OUTPUT INSERTED.generated_col` (SQL Server) / `RETURNING generated_col` (PostgreSQL) inlined in the INSERT itself
* After UPDATE: generator emits a re-SELECT for `[Generated(GenerationTime.Always)]` properties: `SELECT generated_col FROM table WHERE id = @id`
* Re-fetched values are assigned back to the entity instance immediately after mutation
* `[Generated]` and `[Formula]` must not be combined on the same property → `Diagnostic` error
* `[Generated]` and `[Converter]` may be combined — converter is applied to the re-fetched value in the read path
* `[Generated]` on `[MappedSuperclass]` is inherited by all subclasses

***

# 39. Global Custom Filters

## Features

* `[GlobalFilter(Name, Condition)]` on an entity class defines a named conditional WHERE fragment
* `Condition` is a native SQL expression (not CPQL) using `@paramName` for parameter references
* Multiple `[GlobalFilter]` attributes may be applied to one entity
* Filters are activated at runtime via `DapperXOptions.EnableFilter(name, parameters)`
* When a filter is active for a request, its WHERE fragment is appended to all SELECT/UPDATE/DELETE SQL for the entity
* Inactive filters have zero SQL overhead — their template constant is simply not appended

```csharp
[Entity]
[GlobalFilter("active_region", "region = @region")]
[GlobalFilter("tenant_group", "tenant_group_id = @groupId")]
public class Product { … }

// At runtime (e.g., in middleware):
DapperXOptions.EnableFilter("active_region", new { region = "US" });
DapperXOptions.EnableFilter("tenant_group", new { groupId = 42 });
```

***

## Requirements

* `Condition` SQL fragment is a **compile-time constant** stored as a string literal in generated code — not assembled at runtime
* Generator emits one `private static readonly string FILTER_{NAME}` constant per `[GlobalFilter]` per entity
* At runtime: `if (options.IsFilterActive("name")) sql += FILTER_{NAME};` — only the conditional append is runtime; the SQL fragment itself is a compile-time literal
* Filter parameter names in `Condition` must be unique across all active filters for a method call — developer is responsible; DapperX does not validate cross-filter parameter name conflicts at compile time
* `[GlobalFilter]` is distinct from `[SoftDelete]` (always-on) and `[TenantId]` (always-on) — global filters are opt-in per request
* `[GlobalFilter]` on `[MappedSuperclass]` is inherited by all subclasses
* Filter activation is thread-local or scoped to `DapperXOptions` instance — never static global state; developers use scoped DI to inject per-request options
* `DisableFilter(name)` removes a previously activated filter

***

# 40. Final Definition

DapperX is a compile-time generated data access framework that provides full JPA-equivalent entity mapping, relationships, derived query methods, CPQL, window functions, auditing, soft delete, multi-tenancy, lifecycle hooks, composite keys, element collections, named entity graphs, column transformers, global custom filters, generated columns, and query capabilities — transformed into efficient, explicit, batch-oriented SQL execution pipelines without relying on runtime ORM behavior, reflection, change tracking, or identity maps.

***

# Final Insight

DapperX does not manage state.
It guarantees correctness by executing well-defined plans generated entirely at compile time.
