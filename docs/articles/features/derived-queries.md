# Derived Queries

Declare a method on a `[Repository]` interface following a Spring-Data-style naming convention, and DapperX
parses the name at compile time into a full SQL query — no method body, no attribute required:

```csharp
[Repository]
public interface ICatalogProductRepository : IRepository<CatalogProduct, int>
{
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByInStockAndPriceLessThanAsync(bool inStock, decimal price, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryOrderByPriceDescAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, Sort sort, Pageable pageable, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryLockedAsync(string category, LockMode lockMode, CancellationToken ct = default);
}
```

## Grammar

A method name is: **subject** + `By` + one or more **property + operator** segments joined by `And`/`Or`,
optionally followed by `OrderBy<Property>[Asc|Desc]`.

**Subjects** (before `By`): `Find`, `Get`, `Query`, `Search`, `Read`, `Stream`, `Count`, `Exists`, `Has`,
`Contains`, `Delete`, `Remove`, `Insert`, `Add`, `Save`, `Create`, `Update`, `Modify`.

**Operators** (after a property name): comparison (`Is`, `Not`, `GreaterThan`, `LessThan`, `Between`, ...),
string matching (`Like`, `Containing`/`Contains`, `StartingWith`, `EndingWith`), collections (`In`, `NotIn`),
null checks (`Null`/`IsNull`, `NotNull`), boolean (`True`, `False`), date (`Before`, `After`), and regex
(`Regex`/`Matches`) — each with an `Is`-prefixed alias (e.g. `IsGreaterThan`). Combine predicates with `And`/
`Or`, and append `IgnoreCase` for case-insensitive string comparisons.

A trailing `Sort`/`Pageable` parameter (as in `FindByCategoryAsync(string category, Sort sort, Pageable pageable, ...)`)
adds runtime sorting/paging on top of the derived predicate — see [Paging & Sorting](paging-and-sorting.md). A
trailing `LockMode` parameter applies a row lock to the generated SELECT.

The full keyword table lives in `DapperX.Generator.MethodNameParsing.OperatorKeywordTable` — if a mapped
property happens to share a name with an operator keyword, the generator raises a `DPX015` diagnostic at
compile time rather than silently misparsing the method name.

## Compile-time validation

Because parsing happens at compile time, a derived query method that doesn't type-check against the entity's
properties (a typo'd property name, a mismatched parameter type/count) fails the build with a diagnostic —
there's no way to ship a derived query that throws a runtime "unknown member" error.

## When to reach for CPQL instead

Derived queries cover common filter/sort/paging shapes from the method name alone. For anything with joins
across relations, aggregates, or a `SELECT` shape derived queries can't express, use
[`[Query]`/`[NamedQuery]` with CPQL](cpql.md) instead.
