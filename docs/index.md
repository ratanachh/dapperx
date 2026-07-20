---
_disableToc: true
---

# DapperX

A compile-time, source-generator-powered data access framework for .NET, built on top of
[Dapper](https://github.com/DapperLib/Dapper). DapperX lets you define entities and repository interfaces
with attributes and get fully-typed, reflection-free repository implementations generated at build time —
no runtime reflection, no change tracking, no dynamic SQL.

```bash
dotnet add package Ratana.DapperX
dotnet add package Ratana.DapperX.Generator
```

## Features

- **Entity mapping** — `[Entity]`, `[Table]`, `[Id]`, `[GeneratedValue]`, `[Column]`, `[Version]`, `[Embeddable]`, `[MappedSuperclass]`, and more
- **Relations** — `[OneToOne]`, `[OneToMany]`, `[ManyToOne]`, `[ManyToMany]` with lazy-loaded collections/references
- **Batch & graph operations** — `InsertManyAsync`/`UpdateManyAsync`/`DeleteManyAsync`, `InsertGraphAsync`/`UpdateGraphAsync`/`DeleteGraphAsync` with dependency-graph-ordered execution
- **Derived query methods** — query methods parsed from method names (`FindByCategoryAndInStockAsync(...)`)
- **CPQL** — a compact query language for `[NamedQuery]`/`[Query]` methods, resolved to SQL at compile time
- **Paging & sorting** — `Page<T>`, `Slice<T>`, `Pageable`, `Sort`
- **Soft delete, multi-tenancy & auditing** — `[SoftDelete]`, `[TenantId]`, `[GlobalFilter]`, `[CreatedDate]`/`[LastModifiedDate]`/`[CreatedBy]`/`[LastModifiedBy]`
- **Lifecycle hooks** — `[PrePersist]`, `[PostPersist]`, `[PreUpdate]`, `[PostUpdate]`, `[PreRemove]`, `[PostRemove]`, `[PostLoad]`
- **Multi-provider** — SQL Server, PostgreSQL, MySQL, and SQLite

## Quickstart

### 1. Set the compile-time provider

Add the `DapperXDatabaseProvider` property to your `.csproj`:

```xml
<PropertyGroup>
  <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
</PropertyGroup>
```

This property determines which SQL dialect the generator produces **at compile time**. Switching providers requires a rebuild.

### Example project file

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    ...
    <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ratana.DapperX" Version="0.1.4" />
    <PackageReference Include="Ratana.DapperX.Generator" Version="0.1.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

Valid values: `SqlServer` (default), `PostgreSql`, `MySql`, `Sqlite`

### 2. Define an entity

```csharp
using DapperX.Core.Attributes;
using DapperX.Core.Enums;

[Entity]
[Table("catalog_products")]
public class CatalogProduct
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }

    [Column]
    public string Sku { get; set; } = string.Empty;

    [Column]
    public string Name { get; set; } = string.Empty;

    [Column]
    public string Category { get; set; } = string.Empty;

    [Column]
    public decimal Price { get; set; }

    [Column(Name = "in_stock")]
    public bool InStock { get; set; } = true;
}
```

### 3. Define a repository interface

```csharp
using DapperX.Abstractions.Repositories;
using DapperX.Core.Attributes;

[Repository]
public interface ICatalogProductRepository : IRepository<CatalogProduct, int>
{
    Task<IReadOnlyList<CatalogProduct>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<CatalogProduct>> FindByInStockAndPriceLessThanAsync(bool inStock, decimal price, CancellationToken ct = default);
}
```

### 4. Register DapperX

```csharp
using DapperX.Runtime.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDapperX(builder.Configuration.GetConnectionString);

var app = builder.Build();
```

The generator also emits `DapperXConnectionFactory`, which creates the provider-specific `IDbConnection`
for the compile-time `DapperXDatabaseProvider` value.

### 5. Use the generated repository

```csharp
public class CatalogService(ICatalogProductRepository products)
{
    public Task<IReadOnlyList<CatalogProduct>> GetAvailableAsync(string category, CancellationToken ct)
        => products.FindByInStockAndPriceLessThanAsync(true, 100m, ct);
}
```

## Where to next

- **[Getting Started](articles/getting-started.md)** — full setup and quickstart walkthrough
- **[Feature Guides](articles/features/index.md)** — one page per feature area: relations, batch/graph operations, derived queries, CPQL, paging & sorting, soft delete/multi-tenancy/auditing, lifecycle hooks, and providers
- **[Demo App Walkthrough](articles/demo-app.md)** — a runnable ASP.NET Core minimal API exercising nearly every feature
- **[API Reference](api/index.md)** — generated reference for the public types in `DapperX`

DapperX is [MIT licensed](https://github.com/ratanachh/dapperx/blob/main/LICENSE). The package is published
on [NuGet as `Ratana.DapperX`](https://www.nuget.org/packages/Ratana.DapperX).
