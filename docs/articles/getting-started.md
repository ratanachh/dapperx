# Getting Started

## Installation

```bash
dotnet add package Ratana.DapperX
dotnet add package Ratana.DapperX.Generator
```

DapperX targets `net10.0` and requires no runtime dependency beyond [Dapper](https://github.com/DapperLib/Dapper) itself.

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

## 1. Define an entity

Annotate a plain class with `[Entity]` and `[Table]`, mark its primary key with `[Id]`, and mark mapped
properties with `[Column]`:

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

No base class, no change tracking, no proxying — `CatalogProduct` is a plain object DapperX reads and writes
by reflection-free, compile-time-generated ADO.NET code. See [Entity Mapping](features/entity-mapping.md) for
the rest of the mapping attributes (`[Version]`, `[Embeddable]`, `[MappedSuperclass]`, and more).

## 2. Define a repository interface

Declare an interface extending `IRepository<TEntity, TId>` and annotate it with `[Repository]`. DapperX's
source generator emits a sealed implementation at compile time — you never write the CRUD SQL yourself:

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

`IRepository<T, TId>` already gives you `GetByIdAsync`, `GetAllAsync` (with paging/sorting), `InsertAsync`,
`UpdateAsync`, `DeleteAsync`, batch and graph variants, and a fluent `Query()` — see the
[API reference](xref:DapperX.Abstractions.Repositories.IRepository`2) for the full contract. The two extra
methods above are **derived queries**: DapperX parses `FindByCategoryAsync` and
`FindByInStockAndPriceLessThanAsync` from their method names and generates the matching SQL at compile time.
See [Derived Queries](features/derived-queries.md).

## 3. Register DapperX

Call `AddDapperX` with configuration or a connection factory. This extension method — along with the
`{Name}RepositoryImpl` class backing every `[Repository]` interface — is emitted by the generator per
consuming project, so every repository interface it finds is wired up for DI automatically:

The generator also emits `DapperX.Generated.DapperXConnectionFactory`, which creates and opens the
provider-specific `IDbConnection` selected by `DapperXDatabaseProvider`.

```csharp
using DapperX.Runtime.Configuration;
using DapperX.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDapperX(builder.Configuration.GetConnectionString); // uses provider-default connection-string name
// or: builder.Services.AddDapperX(_ => DapperXConnectionFactory.CreateOpenConnection(connectionString));

var app = builder.Build();
```

## 4. Use the generated repository

Inject `ICatalogProductRepository` like any other service — the concrete `CatalogProductRepositoryImpl` is
resolved for you:

```csharp
public class CatalogService(ICatalogProductRepository products)
{
    public Task<IReadOnlyList<CatalogProduct>> GetAvailableAsync(string category, CancellationToken ct)
        => products.FindByInStockAndPriceLessThanAsync(true, 100m, ct);
}
```

## Next steps

- Work through the [Feature Guides](toc.yml) for relations, batch/graph operations, CPQL, paging, soft
  delete/multi-tenancy/auditing, lifecycle hooks, and multi-provider support.
- Run the [Demo App Walkthrough](demo-app.md) — a full ASP.NET Core minimal API exercising nearly every
  attribute in this library, runnable with or without Docker.
- Browse the [API Reference](../api/index.md) for the complete public surface.
