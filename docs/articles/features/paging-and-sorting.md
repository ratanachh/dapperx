# Paging & Sorting

## Sorting

[`Sort`](xref:DapperX.Abstractions.Sorting.Sort) is a simple column + direction record:

```csharp
var sort = new Sort("Sku");            // ascending by default
var descSort = new Sort("Price", Ascending: false);

var products = await repo.GetAllAsync(sort);
```

`Sort.Column` names an entity property, not a raw SQL column — DapperX resolves the mapped column name at
compile/query-build time. Only properties marked `[Sortable]` may be referenced; sorting by an unmapped or
non-sortable property is rejected (see `InvalidSortException`).

## Paging with a total count

[`Pageable`](xref:DapperX.Abstractions.Paging.Pageable) is a page-number/page-size request;
[`Page<T>`](xref:DapperX.Abstractions.Paging.Page`1) is the response, including a `TotalElements` count (and a
derived `TotalPages`):

```csharp
var page = await repo.GetAllAsync(new Pageable(PageNumber: 0, PageSize: 20));
// page.Content, page.TotalElements, page.TotalPages

var sortedPage = await repo.GetAllAsync(sort, new Pageable(0, 20));
```

Computing `TotalElements` requires DapperX to run an extra `COUNT` query alongside the page query.

## Paging without a count: `Slice<T>`

When you don't need the total row count — e.g. "load more" pagination —
[`Slice<T>`](xref:DapperX.Abstractions.Paging.Slice`1) avoids the COUNT query entirely. The generator fetches
`pageSize + 1` rows and sets `HasNext` if the extra row came back:

```csharp
var slice = await repo.GetAllSliceAsync(new Pageable(0, 20));
// slice.Content (at most 20 rows), slice.HasNext
```

## Via the fluent query API

`Skip`/`Take` on [`IQuery<T>`](xref:DapperX.Abstractions.Query.IQuery`1) apply an offset/limit directly, or
call the terminal `ToPageAsync`/`ToSliceAsync` (or `AsSlice()` before a paged terminal call) for the same
count-vs-no-count tradeoff as the repository-level methods:

```csharp
var page = await repo.Query()
    .Where(p => p.Category == category)
    .OrderByDescending(p => p.Price)
    .ToPageAsync(new Pageable(0, 20));
```

## Via derived queries

A trailing `Sort`/`Pageable` parameter on a derived query method applies sorting/paging on top of the parsed
predicate — see [Derived Queries](derived-queries.md) — or encode the sort directly in the method name with
an `OrderBy<Property>[Asc|Desc]` suffix, as in `FindByCategoryOrderByPriceDescAsync`.
