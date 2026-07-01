using Dapper;
using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Sorting;
using DapperX.IntegrationTests.Shared.Fixtures;

namespace DapperX.IntegrationTests.Shared.Scenarios;

public class IntegAllProvidersTests
{
    [Fact]
    public async Task Crud_roundtrip_on_catalog()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var item = new IntegCatalogItem { Id = 1, Sku = "INT-001" };
        await env.Catalog.InsertAsync(item);
        Assert.Equal(1, item.Id);

        env.SqlCounter.Reset();
        var loaded = await env.Catalog.GetByIdAsync(item.Id);
        env.SqlCounter.AssertSqlCallCount(1);
        Assert.NotNull(loaded);
        Assert.Equal("INT-001", loaded!.Sku);

        loaded.Sku = "INT-002";
        await env.Catalog.UpdateAsync(loaded);
        await env.Catalog.DeleteByIdAsync(loaded.Id);
        var gone = await env.Catalog.GetByIdAsync(loaded.Id);
        Assert.Null(gone);
    }

    [Fact]
    public async Task Upsert_roundtrip()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var item = new IntegCatalogItem { Id = 10, Sku = "UPSERT-1" };
        await env.Catalog.UpsertAsync(item);
        Assert.Equal(10, item.Id);
        item.Sku = "UPSERT-2";
        await env.Catalog.UpsertAsync(item);
        var loaded = await env.Catalog.GetByIdAsync(item.Id);
        Assert.Equal("UPSERT-2", loaded!.Sku);
    }

    [Fact]
    public async Task GetAllSlice_issues_single_select_without_count()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        for (var i = 0; i < 3; i++)
            await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 100 + i, Sku = $"SLICE-{i}" });

        env.SqlCounter.Reset();
        var slice = await env.Catalog.GetAllSliceAsync(new Pageable(0, 2));
        env.SqlCounter.AssertSqlCallCount(1);
        Assert.True(slice.Content.Count() <= 2);
    }

    [Fact]
    public async Task Soft_delete_hides_row_until_hard_delete()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var row = new IntegArchivedItem { Id = 1, Name = "keep" };
        await env.Archived.InsertAsync(row);
        await env.Archived.DeleteByIdAsync(row.Id);
        var hidden = await env.Archived.GetByIdAsync(row.Id);
        Assert.Null(hidden);
    }

    [Fact]
    public async Task Tenancy_filters_rows_by_provider_tenant()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.TenantItems.InsertAsync(new IntegTenantItem { Id = 1, Name = "tenant-a" });
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
#if DAPPERX_PROVIDER_SQLITE
        var count = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_tenant_items WHERE tenant_id = @tenantId",
            new { tenantId = tenantId.ToString() });
#else
        var count = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_tenant_items WHERE tenant_id = @tenantId",
            new { tenantId });
#endif
        Assert.Equal(1, count);

        var loaded = await env.TenantItems.GetByIdAsync(1);
        Assert.NotNull(loaded);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), loaded!.TenantId);
    }

    [Fact]
    public async Task Auditing_populates_audit_columns_on_insert()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var row = new IntegAuditedItem { Id = 1, Name = "audited" };
        await env.Audited.InsertAsync(row);
        var createdBy = await env.Connection.ExecuteScalarAsync<string>(
            "SELECT created_by FROM integ_audited WHERE id = 1");
        Assert.Equal("integration-test", createdBy);
    }

    [Fact]
    public async Task InsertMany_uses_bounded_sql_calls()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var rows = Enumerable.Range(1, 1000)
            .Select(i => new IntegBulkRow { Id = i, Code = $"B{i}" })
            .ToList();

        env.SqlCounter.Reset();
#if DAPPERX_PROVIDER_SQLITE
        await env.Bulk.InsertManyAsync(rows, bulkThreshold: 10_000);
        Assert.True(env.SqlCounter.SqlCallCount <= 20, $"Expected batch chunks, got {env.SqlCounter.SqlCallCount}");
#else
        await env.Bulk.InsertManyAsync(rows, bulkThreshold: 100);
        Assert.True(env.SqlCounter.SqlCallCount <= 3, $"Expected O(1) bulk, got {env.SqlCounter.SqlCallCount}");
#endif
    }

    [Fact]
    public async Task LoadChildrenForMany_issues_one_sql_for_relationship()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var p1 = new IntegParent { Id = 1, Name = "p1" };
        var p2 = new IntegParent { Id = 2, Name = "p2" };
        await env.Parents.InsertAsync(p1);
        await env.Parents.InsertAsync(p2);
        var childRepo = new global::DapperX.IntegrationTests.Shared.Fixtures.Generated.IntegChildRepositoryImpl(env.Connection, env.Options);
        await childRepo.InsertAsync(new IntegChild { Id = 11, ParentId = 1, Label = "c1" });
        await childRepo.InsertAsync(new IntegChild { Id = 21, ParentId = 2, Label = "c2" });

        env.SqlCounter.Reset();
        await env.Parents.LoadChildrenForManyAsync([p1, p2]);
        env.SqlCounter.AssertSqlCallCount(1);
        Assert.Equal(2, p1.Children.TryGet()!.Count + p2.Children.TryGet()!.Count);
    }
}
