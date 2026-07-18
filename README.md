# DapperX

[![NuGet](https://img.shields.io/nuget/v/Ratana.DapperX.svg)](https://www.nuget.org/packages/Ratana.DapperX)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A compile-time, source-generator-powered data access framework for .NET, built on top of [Dapper](https://github.com/DapperLib/Dapper). DapperX lets you define entities and repository interfaces without writing SQL — the source generator handles the rest.

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

## Installation

```bash
dotnet add package Ratana.DapperX
dotnet add package Ratana.DapperX.Generator
```

## Configuration

Every project using DapperX **must** configure one setting in its `.csproj` file:

### 1. Set the compile-time database provider

Add the `DapperXDatabaseProvider` property to your `<PropertyGroup>`:

```xml
<PropertyGroup>
  <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
</PropertyGroup>
```

**Valid values:** `SqlServer` (default), `PostgreSql`, `MySql`, `Sqlite`

This property determines which SQL dialect the generator produces **at compile time**. Switching providers requires a rebuild.

### Example project file

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    ...
    <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="Ratana.DapperX" Version="0.1.1" />
    <PackageReference Include="Ratana.DapperX.Generator" Version="0.1.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

**Note:** If using the NuGet package `Ratana.DapperX`, these settings are configured automatically via a `.targets` file and do not require manual setup.

## Quickstart

### 1. Set the compile-time provider

Add the `DapperXDatabaseProvider` property to your `.csproj`:

```xml
<PropertyGroup>
  <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
</PropertyGroup>
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

## Sample application

A runnable ASP.NET Core minimal API demonstrating CRUD, derived queries, `IQuery`, global filters, soft delete, multi-tenancy, auditing, secondary tables, batch/graph insert, locking, and paging in [`samples/DapperX.SampleApp`](samples/DapperX.SampleApp).

## License

[MIT](LICENSE)
