namespace DapperX.Tests;

public class GraphCascadeGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void OrderRepositoryImpl_insert_graph_cascades_items_with_cascade_all()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);
        Assert.Contains("ItemsRepo.InsertManyAsync", body);
    }

    [Fact]
    public void GraphParentRepositoryImpl_insert_graph_skips_children_when_cascade_none()
    {
        var source = ReadGenerated("GraphParentRepositoryImpl.g.cs");
        Assert.Contains("InsertGraphAsync", source);
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);
        Assert.DoesNotContain("ChildrenRepo", body);
        Assert.DoesNotContain("InsertManyAsync", body);
    }

    [Fact]
    public void GraphParentRepositoryImpl_has_no_execution_plan_when_no_cascade()
    {
        var source = ReadGenerated("GraphParentRepositoryImpl.g.cs");
        Assert.DoesNotContain("InsertGraphExecutionPlan", source);
        Assert.Contains("InsertGraphAsync", source);
    }

    [Fact]
    public void OrderRepositoryImpl_execution_plan_lists_only_cascade_children()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        Assert.Contains("InsertGraphExecutionPlan", source);
        Assert.Contains("OrderItem", source);
        Assert.DoesNotContain("GraphChild", source);
    }

    [Fact]
    public void TenantOrderRepositoryImpl_passes_tenant_provider_to_child_repo_in_insert_graph()
    {
        var source = ReadGenerated("TenantOrderRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);
        Assert.Contains("TenantOrderLineRepositoryImpl(_connection, _options, _tenantProvider)", body);
    }
}
