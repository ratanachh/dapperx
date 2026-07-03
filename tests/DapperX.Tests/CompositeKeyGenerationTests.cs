namespace DapperX.Tests;

public class CompositeKeyGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void CompositeOrderItemRepositoryImpl_emits_composite_select_by_id_sql()
    {
        var source = ReadGenerated("CompositeOrderItemRepositoryImpl.g.cs");
        Assert.Contains("SelectByIdSql", source);
        Assert.Contains("order_id = @OrderId AND product_id = @ProductId", source);
    }

    [Fact]
    public void CompositeOrderItemRepositoryImpl_emits_composite_delete_by_id_sql()
    {
        var source = ReadGenerated("CompositeOrderItemRepositoryImpl.g.cs");
        Assert.Contains("DeleteByIdSql", source);
        Assert.Contains("order_id = @OrderId AND product_id = @ProductId", source);
    }

    [Fact]
    public void CompositeOrderItemRepositoryImpl_get_by_id_uses_composite_key_params()
    {
        var source = ReadGenerated("CompositeOrderItemRepositoryImpl.g.cs");
        Assert.Contains("GetByIdAsync(global::DapperX.Tests.Fixtures.CompositeOrderItemId id", source);
        Assert.Contains("OrderId = id.OrderId", source);
        Assert.Contains("ProductId = id.ProductId", source);
        Assert.DoesNotContain("new { id }", source);
    }

    [Fact]
    public void CompositeOrderItemRepositoryImpl_uses_composite_key_type_in_base_class()
    {
        var source = ReadGenerated("CompositeOrderItemRepositoryImpl.g.cs");
        Assert.Contains("DapperXRepositoryBase<DapperX.Tests.Fixtures.CompositeOrderItem, global::DapperX.Tests.Fixtures.CompositeOrderItemId>", source);
    }
}
