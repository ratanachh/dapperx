---
uid: DapperX.Core
summary: *content
---
The compile-time mapping surface the source generator reads: the `[Entity]`/`[Column]`/`[OneToMany]`-style
attributes you annotate your classes with, the metadata models the generator parses them into, and the
enums those attributes accept.

- [`DapperX.Core.Attributes`](xref:DapperX.Core.Attributes) — `[Entity]`, `[Column]`, `[Id]`, `[OneToMany]`, and every other mapping attribute
- [`DapperX.Core.Models`](xref:DapperX.Core.Models) — parsed metadata (`EntityMetadata`, `PropertyMetadata`, `RelationshipMetadata`, and so on)
- [`DapperX.Core.Enums`](xref:DapperX.Core.Enums) — `CascadeType`, `FetchType`, `GenerationType`, `LockMode`, and other attribute-argument enums
- [`DapperX.Core.Configuration`](xref:DapperX.Core.Configuration) — `DapperXDatabaseProviderAttribute`
