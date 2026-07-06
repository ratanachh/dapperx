# Entity Mapping

## Basic mapping

```csharp
[Entity]
[Table("catalog_products")]
public class CatalogProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    [Sortable]
    public string Sku { get; set; } = string.Empty;

    [Column]
    public decimal Price { get; set; }

    [Column(Name = "in_stock")]
    public bool InStock { get; set; } = true;
}
```

- [`[Entity]`](xref:DapperX.Core.Attributes.EntityAttribute) marks the class for repository generation.
- [`[Table]`](xref:DapperX.Core.Attributes.TableAttribute) maps it to a table (optionally in a specific schema).
- [`[Id]`](xref:DapperX.Core.Attributes.IdAttribute) marks the primary key; pair with
  [`[GeneratedValue]`](xref:DapperX.Core.Attributes.GeneratedValueAttribute) for database- or generator-assigned
  identifiers (e.g. `GenerationType.Identity` for an identity/auto-increment column).
- [`[Column]`](xref:DapperX.Core.Attributes.ColumnAttribute) maps a property to a column, with `Name`,
  `Nullable`, `Insertable`, `Updatable`, `Unique`, `Length`/`Precision`/`Scale`, and `ColumnDefinition` for DDL
  overrides.
- `[Sortable]` allows a column to be referenced by a `Sort`/derived-query `OrderBy` clause.

## Auditing and optimistic concurrency via `[MappedSuperclass]`

Share common columns across entities with a `[MappedSuperclass]` base — DapperX flattens its properties into
each derived entity's mapping instead of generating a separate table:

```csharp
[MappedSuperclass]
public abstract class BaseEntity
{
    [CreatedDate]
    [Column(Name = "created_at")]
    public DateTime CreatedAt { get; set; }

    [LastModifiedDate]
    [Column(Name = "modified_at")]
    public DateTime ModifiedAt { get; set; }

    [CreatedBy]
    [Column(Name = "created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [LastModifiedBy]
    [Column(Name = "modified_by")]
    public string ModifiedBy { get; set; } = string.Empty;

    [Version]
    [Column(Name = "row_version")]
    public int RowVersion { get; set; }
}
```

`[Version]` enables optimistic concurrency: generated `UpdateAsync` calls include the version column in the
WHERE clause and increment it, throwing if another writer updated the row first. See
[Soft Delete, Multi-Tenancy & Auditing](soft-delete-tenancy-auditing.md) for how `[CreatedBy]`/`[LastModifiedBy]`
resolve the "current principal".

## Secondary tables

Split a single entity's columns across two tables (e.g. a hot/cold split, or a legacy profile table) with
[`[SecondaryTable]`](xref:DapperX.Core.Attributes.SecondaryTableAttribute), and route individual properties to
it with `[Column(Table = "...")]`:

```csharp
[Entity]
[Table("members")]
[SecondaryTable("member_profiles", PrimaryKeyJoinColumn = "member_id")]
public class Member
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Email { get; set; } = string.Empty;

    [Column(Table = "member_profiles")]
    public string Bio { get; set; } = string.Empty;
}
```

DapperX generates an INSERT/UPDATE against both tables (in a single transaction) and a JOIN for reads.

## Other mapping attributes

- `[Embeddable]` — mark a type as an embeddable value object whose properties are flattened into the owning
  entity's columns (paired with `[Embedded]` on the owning property).
- `[Immutable]` — marks an entity as never updated after insert (generator omits UPDATE/version support).
- `[Formula]` — a read-only computed column backed by a raw SQL expression, evaluated on every SELECT:
  `[Formula("(SELECT COUNT(*) FROM sample_order_lines ol WHERE ol.order_id = sample_orders.id)")]`.
- `[Generated]` — a column whose value is produced by the database itself (`GenerationTime.Insert` or
  `.Always`), read back after the write instead of sent in it.
- `[ColumnTransformer]` — custom read/write SQL expressions for a column (e.g. encrypting a value on write).
- `[Converter]` — a compile-time value converter between the CLR property type and the column type.
