using Dapper;
using Dapper.Npa.Abstractions.Paging;
using Dapper.Npa.Abstractions.Query;
using Dapper.Npa.Abstractions.Sorting;
using Dapper.Npa.Core.Enums;
using Dapper.Npa.IntegrationTests.Shared.Fixtures;

namespace Dapper.Npa.IntegrationTests.Shared.Scenarios;

public class IntegRuntimeExtrasTests
{
    [Fact]
    public async Task Global_filter_with_parameters_restricts_rows()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.TenantRegionUsers.InsertAsync(new IntegTenantRegionUser { Email = "us@test", Region = "US" });
        await env.TenantRegionUsers.InsertAsync(new IntegTenantRegionUser { Email = "eu@test", Region = "EU" });

        var all = (await env.TenantRegionUsers.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);

        env.Options.EnableFilter("active_region", new { region = "US" });
        var filtered = (await env.TenantRegionUsers.GetAllAsync()).ToList();
        Assert.Single(filtered);
        Assert.Equal("us@test", filtered[0].Email);

        env.Options.DisableFilter("active_region");
        var restored = (await env.TenantRegionUsers.GetAllAsync()).ToList();
        Assert.Equal(2, restored.Count);
    }

    [Fact]
    public async Task Global_filter_IQuery_include_deleted_with_parameters()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        env.Options.EnableFilter("active_region", new { region = "US" });
        await env.TenantRegionUsers.InsertAsync(new IntegTenantRegionUser { Email = "us@test", Region = "US" });
        await env.TenantRegionUsers.InsertAsync(new IntegTenantRegionUser { Email = "eu@test", Region = "EU" });

        var rows = (await env.TenantRegionUsers.Query()
            .IncludeDeleted()
            .ToListAsync()).ToList();
        Assert.Single(rows);
        Assert.Equal("us@test", rows[0].Email);
    }

    [Fact]
    public async Task Tenant_region_user_soft_delete_by_id()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        env.Options.EnableFilter("active_region", new { region = "US" });

        var user = new IntegTenantRegionUser { Email = "delete@test", Region = "US" };
        await env.TenantRegionUsers.InsertAsync(user);
        Assert.True(user.Id > 0);

        await env.TenantRegionUsers.DeleteByIdAsync(user.Id);

        var active = await env.TenantRegionUsers.GetByIdAsync(user.Id);
        Assert.Null(active);

        var deleted = await env.TenantRegionUsers.GetByIdAsync(user.Id, includeDeleted: true);
        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);
    }

#if DAPPERX_PROVIDER_SQLSERVER
    [Fact]
    public async Task GetAllAsync_sort_and_page_sqlserver()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 1, Sku = "B" });
        await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 2, Sku = "A" });

        env.SqlCounter.Reset();
        var page = await env.Catalog.GetAllAsync(new Sort("Sku"), new Pageable(0, 10));
        env.SqlCounter.AssertSqlCallCount(2);

        Assert.Equal(2, page.TotalElements);
        Assert.Equal("A", page.Content[0].Sku);
        Assert.Equal("B", page.Content[1].Sku);
    }
#endif

    [Fact]
    public async Task Global_filter_EnableFilter_restricts_select_rows()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.FilteredCatalog.InsertAsync(new IntegFilteredCatalogItem { Id = 1, Sku = "ON", IsActive = true });
        await env.FilteredCatalog.InsertAsync(new IntegFilteredCatalogItem { Id = 2, Sku = "OFF", IsActive = false });

        var all = (await env.FilteredCatalog.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);

        env.Options.EnableFilter("ActiveOnly");
        var filtered = (await env.FilteredCatalog.GetAllAsync()).ToList();
        Assert.Single(filtered);
        Assert.Equal("ON", filtered[0].Sku);

        env.Options.DisableFilter("ActiveOnly");
        var restored = (await env.FilteredCatalog.GetAllAsync()).ToList();
        Assert.Equal(2, restored.Count);
    }

    [Fact]
    public async Task ColumnTransformer_roundtrip_persists_display_name()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Connection.ExecuteAsync(
            "INSERT INTO integ_transform_products (id, name) VALUES (@id, @name)",
            new { id = 7, name = "Widget" });

        var loaded = await env.TransformProducts.GetByIdAsync(7);
        Assert.NotNull(loaded);
        Assert.Equal("Widget", loaded!.DisplayName);

        loaded.DisplayName = "Gadget";
        await env.TransformProducts.UpdateAsync(loaded);
        var reloaded = await env.TransformProducts.GetByIdAsync(7);
        Assert.Equal("Gadget", reloaded!.DisplayName);
    }

    [Fact]
    public async Task IQuery_Select_projection_dto_roundtrip()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 30, Sku = "DTO-30" });

        env.SqlCounter.Reset();
        IQuery<IntegCatalogSkuDto> query = env.Catalog.Query().Select<IntegCatalogSkuDto>();
        var rows = (await query.Where(x => x.Sku == "DTO-30").ToListAsync()).ToList();
        env.SqlCounter.AssertSqlCallCount(1);
        Assert.Single(rows);
        Assert.Equal(30, rows[0].Id);
        Assert.Equal("DTO-30", rows[0].Sku);
    }

    [Fact]
    public async Task IQuery_AsSplitQuery_issues_separate_sql_per_include()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.QueryCustomers.InsertAsync(new IntegQueryCustomer { Id = 1, Name = "Acme" });
        await env.QueryProducts.InsertAsync(new IntegQueryProduct { Id = 10, Sku = "Q-10", CustomerId = 1 });

        env.SqlCounter.Reset();
        var rows = (await env.QueryProducts.Query()
            .Include("Customer")
            .AsSplitQuery()
            .Where(x => x.Sku == "Q-10")
            .ToListAsync()).ToList();
        env.SqlCounter.AssertSqlCallCount(2);
        Assert.Single(rows);
        Assert.Equal("Acme", rows[0].Customer.Name);
    }

#if !DAPPERX_PROVIDER_SQLITE
    [Fact]
    public async Task IQuery_PessimisticRead_emits_share_lock_sql()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 40, Sku = "READ-LOCK" });

        env.SqlCounter.Reset();
        _ = await env.Catalog.Query()
            .Where(x => x.Sku == "READ-LOCK")
            .WithLock(LockMode.PessimisticRead)
            .ToListAsync();
        env.SqlCounter.AssertSqlCallCount(1);

        var sql = string.Join(' ', env.SqlCounter.Entries.Select(e => e.Sql));
        switch (env.Provider)
        {
            case "SqlServer":
                Assert.Contains("HOLDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
            case "MySql":
                Assert.Contains("SHARE", sql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider {env.Provider}");
        }
    }
#endif
}
