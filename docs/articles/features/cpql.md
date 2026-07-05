# CPQL

CPQL ("Compile-time Property Query Language") is a JPQL-like query language DapperX parses and translates to
provider-specific SQL entirely at compile time — you write queries against entity property names and
navigation paths, not table/column names, and DapperX resolves the mapping for you.

> The sample app doesn't demonstrate CPQL yet (some generator edge cases are still being resolved for that
> assembly's entity shapes) — the examples below are drawn directly from `tests/DapperX.Tests`.

## Basic usage

Annotate a `[Repository]` method with [`[Query]`](xref:DapperX.Core.Attributes.QueryAttribute), using
CPQL entity/property names rather than table/column names, with `:name`-style named parameters:

```csharp
[Query("SELECT p FROM Product p WHERE p.Name = :name")]
Task<IEnumerable<Product>> FindByNameCpqlAsync(string name);

[Query("SELECT COUNT(p) FROM Product p WHERE p.Name = :name")]
Task<long> CountByNameCpqlAsync(string name);
```

Navigate relations with dot paths — DapperX resolves the join at compile time from the entity's
`[ManyToOne]`/`[OneToOne]`/etc. mapping:

```csharp
[Query("SELECT p FROM Product p WHERE p.Customer.Name = :name")]
Task<IEnumerable<Product>> FindByCustomerNameCpqlAsync(string name);
```

## Native SQL escape hatch

Set `NativeQuery = true` to bypass CPQL parsing entirely and pass raw, provider-specific SQL straight through
(still with named parameter binding):

```csharp
[Query("SELECT id, name FROM products WHERE name = @name", NativeQuery = true)]
Task<IEnumerable<Product>> FindByNameNativeAsync(string name);
```

## Named queries

[`[NamedQuery]`](xref:DapperX.Core.Attributes.NamedQueryAttribute) declares a reusable, named CPQL query at
the class level (or repeated with `AllowMultiple` via `[NamedQueries]`), referenced by name elsewhere instead
of inlining the CPQL string on every method.

## What CPQL supports

Beyond simple `WHERE`/`COUNT` queries, the CPQL pipeline (`DapperX.Generator.Cpql`) supports scalar functions,
window functions, and richer filter expressions — see the corresponding test suites for coverage: filter
expressions (`CpqlFilterTests`), scalar functions (`CpqlScalarSnapshotTests`), window functions
(`CpqlWindowFunctionTests`), and semantic validation of property/navigation paths at compile time
(`CpqlSemanticValidatorTests`) — an invalid path fails the build rather than the query failing at runtime.
