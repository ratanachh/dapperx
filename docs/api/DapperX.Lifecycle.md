---
uid: DapperX.Lifecycle
summary: *content
---
Calls an entity's `[PrePersist]`/`[PostLoad]`-style lifecycle hook methods at the matching point in the
generated repository's insert/update/delete/load pipeline — for both single-entity calls and batches.

- [`DapperX.Lifecycle.Invokers`](xref:DapperX.Lifecycle.Invokers) — `EntityLifecycleInvoker<T>`, `LifecycleInvokerBase<T>`
- [`DapperX.Lifecycle.Batch`](xref:DapperX.Lifecycle.Batch) — `BatchLifecycleInvoker<T>`
