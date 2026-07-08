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
```

The `Ratana.DapperX` package brings in the source generator automatically as a build-time analyzer — there's nothing else to install.

## Configuration

Every project using DapperX **must** configure two settings in its `.csproj` file:

### 1. Set the compile-time database provider

Add the `DapperXDatabaseProvider` property to your `<PropertyGroup>`:

```xml
<PropertyGroup>
  <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
</PropertyGroup>
```

**Valid values:** `SqlServer` (default), `PostgreSql`, `MySql`, `Sqlite`

This property determines which SQL dialect the generator produces **at compile time**. Switching providers requires a rebuild.

### 2. Reference the generator correctly

Add the `DapperX.Generator` project reference with analyzer settings:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/DapperX.csproj" />
  <ProjectReference Include="path/to/DapperX.Generator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

**Why these settings?**
- `OutputItemType="Analyzer"` — tells MSBuild to load the generator only as a Roslyn analyzer at compile time
- `ReferenceOutputAssembly="false"` — prevents the generator assembly from being included in your app's output, keeping dependencies minimal

### Example project file

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <DapperXDatabaseProvider>SqlServer</DapperXDatabaseProvider>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\DapperX\DapperX.csproj" />
    <ProjectReference Include="..\..\src\DapperX.Generator\DapperX.Generator.csproj" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**Note:** If using the NuGet package `Ratana.DapperX`, these settings are configured automatically via a `.targets` file and do not require manual setup.

## Quickstart

Define an entity:

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

Define a repository interface — DapperX generates the implementation at compile time:

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

Register DapperX and hand it a connection factory — every `[Repository]` interface is wired up for you:

```csharp
using DapperX.Runtime.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDapperXRepositories(_ => CreateOpenConnection(connectionString));

var app = builder.Build();
```

Then inject and use the generated repository like any other service:

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
