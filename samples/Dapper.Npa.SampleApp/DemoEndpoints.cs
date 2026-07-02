using System.Data;
using Dapper;
using Dapper.Npa.SampleApp.Entities;
using Dapper.Npa.SampleApp.Infrastructure;
using Dapper.Npa.SampleApp.Repositories;
using Dapper.Npa.Abstractions.Configuration;
using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Sorting;
using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.SampleApp;

internal static class DemoEndpoints
{
    public static void MapDemoEndpoints(this WebApplication app, SampleDatabaseHost dbHost)
    {
        app.MapGet("/", () => Results.Ok(new
        {
            message = "Dapper NpaSample Application",
            provider = dbHost.Provider,
            database = dbHost.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "in-memory SQLite (no Docker)"
                : "Docker Compose SQL Server (localhost:14333)",
            sections = new[] { "catalog", "users", "orders", "org", "graph", "sqlite" },
        }));

        MapCatalogEndpoints(app);
        MapUserEndpoints(app);
        MapMemberEndpoints(app);
        MapOrderEndpoints(app);
        MapOrgEndpoints(app);
        MapGraphEndpoints(app);
        MapSqliteNote(app, dbHost);
    }

    private static void MapCatalogEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/catalog");

        g.MapGet("/", async (ICatalogProductRepository repo) =>
            Results.Ok(await repo.GetAllAsync()));

        g.MapGet("/{id:int}", async (int id, ICatalogProductRepository repo) =>
        {
            var item = await repo.GetByIdAsync(id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        g.MapGet("/ids", async (int[] ids, ICatalogProductRepository repo) =>
            Results.Ok(await repo.FindAllByIdAsync(ids)));

        g.MapGet("/exists/{id:int}", async (int id, ICatalogProductRepository repo) =>
            Results.Ok(new { id, exists = await repo.ExistsByIdAsync(id) }));

        g.MapGet("/count", async (ICatalogProductRepository repo) =>
            Results.Ok(new { count = await repo.CountAsync() }));

        g.MapGet("/page", async (int page, int size, ICatalogProductRepository repo) =>
        {
            var result = await repo.GetAllAsync(new Pageable(page, size));
            return Results.Ok(result);
        });

        g.MapGet("/page/sorted", async (int page, int size, ICatalogProductRepository repo) =>
        {
            var sort = new Sort("Sku");
            var result = await repo.GetAllAsync(sort, new Pageable(page, size));
            return Results.Ok(result);
        });

        g.MapGet("/slice", async (int page, int size, ICatalogProductRepository repo) =>
        {
            var slice = await repo.GetAllSliceAsync(new Pageable(page, size));
            return Results.Ok(new { slice.HasNext, items = slice.Content });
        });

        g.MapGet("/derived/in-stock-cheap", async (decimal maxPrice, ICatalogProductRepository repo) =>
            Results.Ok(await repo.FindByInStockAndPriceLessThanAsync(true, maxPrice)));

        g.MapGet("/derived/by-category-sorted", async (string category, ICatalogProductRepository repo) =>
            Results.Ok(await repo.FindByCategoryOrderByPriceDescAsync(category)));

        g.MapGet("/derived/slice/{category}", async (string category, int page, int size, ICatalogProductRepository repo) =>
        {
            var all = await repo.FindByCategoryAsync(category);
            _ = all;
            var slice = await repo.GetAllSliceAsync(new Pageable(page, size));
            return Results.Ok(new { slice.HasNext, items = slice.Content });
        });

        g.MapPost("/", async (CatalogProduct product, ICatalogProductRepository repo, IDbConnection db) =>
        {
            if (!string.IsNullOrEmpty(product.EncryptedPayload))
            {
                await repo.InsertAsync(product);
                return Results.Json(product, statusCode: StatusCodes.Status201Created);
            }

            await db.ExecuteAsync(
                "INSERT INTO catalog_products (sku, name, category, price, in_stock, status, secret_payload, created_at, updated_at) VALUES (@sku, @name, @category, @price, @inStock, @status, @secret, SYSUTCDATETIME(), SYSUTCDATETIME())",
                new
                {
                    sku = product.Sku,
                    name = product.Name,
                    category = product.Category,
                    price = product.Price,
                    inStock = product.InStock,
                    status = product.Status.ToString(),
                    secret = product.EncryptedPayload,
                });
            product.Id = await db.ExecuteScalarAsync<int>("SELECT CAST(SCOPE_IDENTITY() AS int)");
            var loaded = await repo.GetByIdAsync(product.Id);
            return Results.Json(loaded, statusCode: StatusCodes.Status201Created);
        });

        g.MapPut("/{id:int}", async (int id, CatalogProduct product, ICatalogProductRepository repo) =>
        {
            product.Id = id;
            await repo.UpdateAsync(product);
            return Results.NoContent();
        });

        g.MapDelete("/{id:int}", async (int id, ICatalogProductRepository repo) =>
        {
            await repo.DeleteByIdAsync(id);
            return Results.NoContent();
        });

        g.MapDelete("/bulk", async (int[] ids, ICatalogProductRepository repo) =>
        {
            await repo.DeleteAllByIdAsync(ids);
            return Results.NoContent();
        });

        g.MapPost("/batch", async (CatalogProduct[] items, ICatalogProductRepository repo) =>
        {
            await repo.InsertManyAsync(items);
            return Results.Ok(items);
        });

        g.MapPost("/lock-read/{category}", async (string category, ICatalogProductRepository repo) =>
            Results.Ok(await repo.FindByCategoryLockedAsync(category, LockMode.PessimisticRead)));

        g.MapPost("/lock-update/{category}", async (string category, ICatalogProductRepository repo) =>
            Results.Ok(await repo.Query()
                .Where(p => p.Category == category)
                .WithLock(LockMode.Pessimistic)
                .ToListAsync()));
    }

    private static void MapUserEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/users");

        g.MapGet("/", async (IAppUserRepository repo, IDapperXOptions options) =>
        {
            options.EnableFilter("active_region", new { region = "US" });
            return Results.Ok(await repo.GetAllAsync());
        });

        g.MapPost("/", async (AppUser user, IAppUserRepository repo, IUserProfileRepository profiles) =>
        {
            user.TenantId = SampleTenantProvider.DemoTenantId;
            await repo.InsertAsync(user);
            await profiles.InsertAsync(new UserProfile { Id = user.Id, DisplayName = user.Email });
            SampleAuditListener.Reset();
            return Results.Created($"/demo/users/{user.Id}", user);
        });

        g.MapGet("/{id:int}/profile", async (int id, IUserProfileRepository profiles) =>
        {
            var profile = await profiles.GetByIdAsync(id);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        });

        g.MapDelete("/{id:int}", async (int id, IAppUserRepository repo) =>
        {
            await repo.DeleteByIdAsync(id);
            return Results.Ok(new { softDeleted = true });
        });

        g.MapGet("/include-deleted", async (IAppUserRepository repo) =>
            Results.Ok(await repo.Query().IncludeDeleted().ToListAsync()));

        g.MapGet("/listener-count", () => Results.Ok(new { SampleAuditListener.PrePersistCount }));
    }

    private static void MapMemberEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/members");

        g.MapPost("/", async (Member member, IMemberRepository repo) =>
        {
            await repo.InsertAsync(member);
            return Results.Created($"/demo/members/{member.Id}", member);
        });

        g.MapGet("/{id:int}", async (int id, IMemberRepository repo) =>
        {
            var m = await repo.GetByIdAsync(id);
            return m is null ? Results.NotFound() : Results.Ok(m);
        });
    }

    private static void MapOrderEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/orders");

        g.MapPost("/", async (SalesOrderCreateDto dto, ISalesOrderRepository repo) =>
        {
            var order = new SalesOrder { Code = dto.Code };
            await repo.InsertAsync(order);
            return Results.Created($"/demo/orders/{order.Id}", order);
        });

        g.MapGet("/{id:int}", async (int id, ISalesOrderRepository repo) =>
        {
            var order = await repo.GetByIdAsync(id);
            return order is null ? Results.NotFound() : Results.Ok(order);
        });
    }

    private static void MapOrgEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/org");

        g.MapPost("/departments", async (Department dept, IDepartmentRepository repo) =>
        {
            await repo.InsertAsync(dept);
            return Results.Created($"/demo/org/departments/{dept.Id}", dept);
        });

        g.MapGet("/departments/{id:int}/employees", async (int id, IDbConnection db) =>
        {
            var rows = await db.QueryAsync<Employee>(
                "SELECT id AS Id, department_id AS DepartmentId, employee_code AS EmployeeCode, full_name AS FullName FROM sample_employees WHERE department_id = @id",
                new { id });
            return Results.Ok(rows);
        });
    }

    private static void MapGraphEndpoints(WebApplication app)
    {
        var g = app.MapGroup("/demo/graph");

        g.MapPost("/", async (GraphParentDto dto, IGraphParentRepository repo) =>
        {
            var parent = new GraphParent { Name = dto.Name, Children = new() };
            parent.Children.Set(dto.Children.Select((label, i) => new GraphChild { Label = label }).ToList());
            await repo.InsertGraphAsync(parent);
            return Results.Ok(parent);
        });
    }

    private static void MapSqliteNote(WebApplication app, SampleDatabaseHost dbHost)
    {
        app.MapGet("/demo/sqlite", () => Results.Ok(new
        {
            message = "Default: Docker Compose SQL Server (`docker compose up -d` in samples/Dapper.Npa.SampleApp). Set DapperX:DatabaseProvider to Sqlite for in-memory SQLite without Docker.",
            currentProvider = dbHost.Provider,
            connectionSource = dbHost.Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
                ? "in-memory"
                : "docker-compose",
        }));
    }

    private sealed record GraphParentDto(string Name, IReadOnlyList<string> Children);
    private sealed record SalesOrderCreateDto(string Code);
}
