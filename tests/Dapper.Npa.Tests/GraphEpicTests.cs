namespace Dapper.Npa.Tests;

public class GraphEpicTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void OrderRepositoryImpl_delete_graph_closes_order_gaps_before_child_delete()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        var deleteStart = source.IndexOf("public override async Task DeleteGraphAsync", StringComparison.Ordinal);
        Assert.True(deleteStart >= 0);
        var deleteEnd = source.IndexOf("private static readonly global::Dapper.Npa.Batching.Execution.ExecutionPlan DeleteGraphExecutionPlan", deleteStart, StringComparison.Ordinal);
        var body = source.Substring(deleteStart, deleteEnd - deleteStart);
        Assert.Contains("CloseItemsOrderGapAsync", body);
        Assert.Contains("ItemsRepo.DeleteManyAsync", body);
    }

    [Fact]
    public void BatchGraphParentRepositoryImpl_insert_graph_invokes_batch_lifecycle()
    {
        var source = ReadGenerated("BatchGraphParentRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertGraphAsync", StringComparison.Ordinal);
        var insertEnd = source.IndexOf("public override async Task UpdateGraphAsync", insertStart, StringComparison.Ordinal);
        var body = source.Substring(insertStart, insertEnd - insertStart);
        Assert.Contains("_batchLifecycle.InvokePrePersistBatch", body);
        Assert.Contains("_batchLifecycle.InvokePostPersistBatch", body);
    }

    [Fact]
    public void SoftDeleteGraphOrderRepositoryImpl_delete_graph_uses_soft_delete_on_children()
    {
        var childSource = ReadGenerated("SoftDeleteGraphOrderLineRepositoryImpl.g.cs");
        Assert.Contains("is_deleted = 1", childSource);

        var parentSource = ReadGenerated("SoftDeleteGraphOrderRepositoryImpl.g.cs");
        var deleteStart = parentSource.IndexOf("public override async Task DeleteGraphAsync", StringComparison.Ordinal);
        Assert.True(deleteStart >= 0);
        Assert.Contains("LinesRepo.DeleteManyAsync", parentSource.Substring(deleteStart));
    }

    [Fact]
    public void OrderRepositoryImpl_execution_plan_matches_cascade_filtered_children()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        Assert.Contains("InsertGraphExecutionPlan", source);
        Assert.Contains("OrderItem", source);
        Assert.DoesNotContain("GraphChild", source);
        Assert.DoesNotContain("BatchGraphChild", source);
    }

    [Fact]
    public void ProductRepositoryImpl_named_entity_graph_sql_is_prebaked()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.Contains("Graph_product_withCustomer_Sql", source);
        Assert.Contains("INNER JOIN customers", source);
    }
}
