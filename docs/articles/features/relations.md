# Relations

DapperX relations are declared with attributes and, by default, loaded **lazily** — the collection
or reference isn't fetched until you access it (or explicitly `Include`/`ThenInclude` it in a fluent query).

## Many-to-one / one-to-many

```csharp
[Entity]
[Table("sample_departments")]
public class Department
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Name { get; set; } = string.Empty;

    [OneToMany(MappedBy = nameof(Employee.Department))]
    [MapKey("employee_code")]
    public LazyMap<string, Employee> EmployeesByCode { get; set; } = new(e => e.EmployeeCode);
}

[Entity]
[Table("sample_employees")]
public class Employee
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column(Name = "department_id")]
    public int DepartmentId { get; set; }

    [Column(Name = "employee_code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [ManyToOne]
    [JoinColumn("department_id")]
    public Department Department { get; set; } = null!;
}
```

- [`[ManyToOne]`](xref:DapperX.Core.Attributes.ManyToOneAttribute) is the owning side — pair with
  [`[JoinColumn]`](xref:DapperX.Core.Attributes.JoinColumnAttribute) to name the foreign key column.
- [`[OneToMany]`](xref:DapperX.Core.Attributes.OneToManyAttribute)`(MappedBy = ...)` is the inverse side,
  pointing back at the owning property on the child.
- `LazyMap<TKey, TValue>` (keyed by `[MapKey]`) and `LazyCollection<T>` are the two lazy-relation container
  types; a `LazyReference<T>` equivalent exists for `[OneToOne]`.

## One-to-one

[`[OneToOne]`](xref:DapperX.Core.Attributes.OneToOneAttribute) works the same way — `[JoinColumn]` on the
owning side, `MappedBy` on the inverse side — for a strict 1:1 relation instead of a collection.

## Many-to-many

[`[ManyToMany]`](xref:DapperX.Core.Attributes.ManyToManyAttribute) relations are backed by a join table,
described with `[JoinTable]` (join table name plus the join columns on each side).

## Cascading

Every relation attribute accepts a `Cascade` (`CascadeType`) controlling which lifecycle operations on the
owner propagate to the related entity/entities — e.g. `Cascade = CascadeType.All` so that
`InsertGraphAsync`/`UpdateGraphAsync`/`DeleteGraphAsync` on the parent also persist its children. See
[Batch & Graph Operations](batch-graph-operations.md) for how cascades are executed in dependency order.

## Fetch strategy

Every relation attribute also accepts `Fetch` (`FetchType.Lazy`, the default, or `FetchType.Eager`). Eager
relations are loaded as part of the entity's own query; lazy relations are loaded on first access, or
up-front via `Query().Include("PropertyName")`/`ThenInclude(...)` (optionally `AsSplitQuery()` to load each
include with its own query instead of one large join).

## Ordering a collection

`[OrderColumn("position")]` on a `[OneToMany]`/`[ManyToMany]` collection sorts the loaded collection by that
column, as used by `SalesOrder.Lines` in the sample app.
