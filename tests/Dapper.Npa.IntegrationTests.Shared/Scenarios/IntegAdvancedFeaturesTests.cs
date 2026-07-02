using Dapper;
using Dapper.Npa.IntegrationTests.Shared.Fixtures;

namespace Dapper.Npa.IntegrationTests.Shared.Scenarios;

public class IntegAdvancedFeaturesTests
{
    [Fact]
    public async Task Composite_key_roundtrip()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var key = new IntegCompositeOrderItemId { OrderId = 1, ProductId = 10 };
        await env.CompositeItems.InsertAsync(new IntegCompositeOrderItem { OrderId = 1, ProductId = 10, Quantity = 3 });
        var loaded = await env.CompositeItems.GetByIdAsync(key);
        Assert.NotNull(loaded);
        Assert.Equal(3, loaded!.Quantity);
    }

    [Fact]
    public async Task Secondary_table_persists_summary()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Documents.InsertAsync(new IntegDocument { Id = 1, Title = "doc", Summary = "summary text" });
        var summary = await env.Connection.ExecuteScalarAsync<string>(
            "SELECT summary FROM integ_document_details WHERE document_id = 1");
        Assert.Equal("summary text", summary);
    }

    [Fact]
    public async Task PrimaryKeyJoinColumn_shares_profile_id()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Users.InsertAsync(new IntegUser
        {
            Id = 5,
            Email = "u@example.com",
            Profile = new IntegUserProfile { DisplayName = "display" },
        });
        var profileRepo = new global::Dapper.Npa.IntegrationTests.Shared.Fixtures.Generated.IntegUserProfileRepositoryImpl(env.Connection, env.Options);
        await profileRepo.InsertAsync(new IntegUserProfile { Id = 5, DisplayName = "display" });
        var profileId = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT id FROM integ_user_profiles WHERE id = 5");
        Assert.Equal(5, profileId);
    }

    [Fact]
    public async Task Element_collection_persists_images()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var product = new IntegGalleryProduct { Id = 1, Sku = "G-1" };
        product.Images.Set([new IntegProductImage { Url = "http://img", Caption = "cap" }]);
        await env.Gallery.InsertAsync(product);
        var count = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_product_images WHERE product_id = 1");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task LazyMap_loads_employees_by_code()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var dept = new IntegDepartment { Id = 1, Name = "eng" };
        await env.Departments.InsertAsync(dept);
        var empRepo = new global::Dapper.Npa.IntegrationTests.Shared.Fixtures.Generated.IntegEmployeeRepositoryImpl(env.Connection, env.Options);
        await empRepo.InsertAsync(new IntegEmployee { Id = 10, DepartmentId = 1, EmployeeCode = "E10", FullName = "Ten" });

        await env.Departments.LoadEmployeesByCodeForManyAsync([dept]);
        Assert.True(dept.EmployeesByCode.IsLoaded);
        Assert.Equal("Ten", dept.EmployeesByCode.TryGet()!["E10"].FullName);
    }

    [Fact]
    public async Task Named_entity_graph_loads_order_lines()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.GraphOrders.InsertAsync(new IntegGraphOrder { Id = 1, Code = "GO-1" });
        var lineRepo = new global::Dapper.Npa.IntegrationTests.Shared.Fixtures.Generated.IntegGraphOrderLineRepositoryImpl(env.Connection, env.Options);
        await lineRepo.InsertAsync(new IntegGraphOrderLine { Id = 100, OrderId = 1, Sku = "SKU-1" });

        var rows = (await env.GraphOrders.FindByCodeWithGraphAsync("GO-1", "integGraphOrder.withLines")).ToList();
        Assert.Single(rows);
        Assert.Equal("GO-1", rows[0].Code);
#if DAPPERX_PROVIDER_POSTGRESQL
        var lineCount = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_graph_order_lines WHERE order_id = 1 AND is_deleted = false");
#else
        var lineCount = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_graph_order_lines WHERE order_id = 1 AND is_deleted = 0");
#endif
        Assert.Equal(1, lineCount);
    }

    [Fact]
    public async Task InsertGraph_persists_parent_and_children()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var parent = new IntegGraphParent
        {
            Id = 1,
            Name = "root",
            Children = new(),
        };
        parent.Children.Set([
            new IntegGraphChild { Id = 11, ParentId = 1, Label = "c1" },
            new IntegGraphChild { Id = 12, ParentId = 1, Label = "c2" },
        ]);
        await env.GraphParents.InsertGraphAsync(parent);
        var childCount = await env.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM integ_graph_children WHERE parent_id = 1");
        Assert.Equal(2, childCount);
    }
}
