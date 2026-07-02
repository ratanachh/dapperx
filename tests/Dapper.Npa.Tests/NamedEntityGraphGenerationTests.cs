namespace Dapper.Npa.Tests;

public class NamedEntityGraphGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_emits_named_entity_graph_sql_and_switch()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");

        Assert.Contains("Graph_product_withCustomer_FromSql", source);
        Assert.Contains("Graph_product_withCustomer_Sql", source);
        Assert.Contains("INNER JOIN customers g_Customer ON e.customer_id = g_Customer.id", source);
        Assert.Contains("ResolveNamedEntityGraphSql", source);
        Assert.Contains("ResolveNamedEntityGraphFromSql", source);
        Assert.Contains("InvalidEntityGraphException", source);
        Assert.Contains("LoadGraphAsync", source);
    }

    [Fact]
    public void SoftDeleteGraphOrderRepositoryImpl_graph_sql_includes_soft_delete_on_root_and_joined()
    {
        var source = ReadGenerated("SoftDeleteGraphOrderRepositoryImpl.g.cs");

        Assert.Contains("Graph_softDeleteOrder_withLines_FromSql", source);
        Assert.Contains("INNER JOIN soft_delete_graph_order_lines g_Lines ON", source);
        Assert.Contains("e.is_deleted = 0", source);
        Assert.Contains("g_Lines.is_deleted = 0", source);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_graph_sql_includes_tenant_filter()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");

        Assert.Contains("Graph_tenantItem_default_FromSql", source);
        Assert.Contains("e.tenant_id = @tenantId", source);
    }

    [Fact]
    public void OrderRepositoryImpl_subgraph_joins_items_table()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");

        Assert.Contains("Graph_order_withLines_FromSql", source);
        Assert.Contains("INNER JOIN order_items g_Items ON g_Items.order_id = e.id", source);
    }

    [Fact]
    public void ProductRepositoryImpl_derived_query_uses_entity_graph_from_sql()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");

        Assert.Contains("FindByNameWithGraphAsync", source);
        Assert.Contains("ResolveNamedEntityGraphFromSql(entityGraph)", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_DPX071_entity_graph_with_include()
    {
        var source = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Dapper.Npa.Generator", "Utils", "DiagnosticsReporter.cs")));
        Assert.Contains("\"DPX071\"", source);
        Assert.Contains("EntityGraph cannot combine with Include joins", source);
    }
}
