---
uid: DapperX.Relations
summary: *content
---
How `[OneToOne]`/`[OneToMany]`/`[ManyToOne]`/`[ManyToMany]` relations get loaded on demand: the lazy
wrapper types that sit on the property, and the loaders that fetch the related rows the first time they're
accessed.

- [`DapperX.Relations.Lazy`](xref:DapperX.Relations.Lazy) — `LazyReference<T>`, `LazyCollection<T>`, `LazyMap<TKey, TValue>`
- [`DapperX.Relations.Loaders`](xref:DapperX.Relations.Loaders) — `ReferenceLoader`, `CollectionLoader`, `MapLoader`
- [`DapperX.Relations.Helpers`](xref:DapperX.Relations.Helpers) — `RelationshipHelper`
