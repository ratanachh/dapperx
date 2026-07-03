using DapperX.Abstractions.Paging;
using DapperX.Abstractions.Repositories;
using DapperX.Abstractions.Sorting;
using DapperX.Runtime.Repositories;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class GetAllAsyncGenerationTests
{
    private static string ReadGeneratedProductRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_inherits_GetAllAsync_overloads_from_base()
    {
        var baseMethods = typeof(DapperXRepositoryBase<Product, int>)
            .GetMethods()
            .Where(m => m.Name == nameof(IRepository<Product, int>.GetAllAsync))
            .Select(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)))
            .OrderBy(s => s)
            .ToList();

        var implMethods = typeof(ProductRepositoryImpl)
            .GetMethods()
            .Where(m => m.Name == nameof(IRepository<Product, int>.GetAllAsync))
            .Select(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)))
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(baseMethods, implMethods);
    }

    [Fact]
    public void ProductRepositoryImpl_emits_sort_fragments_and_GetSortFragment_override()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("file static class ProductSortFragments", source);
        Assert.Contains("protected override string GetSortFragment(Sort sort)", source);
        Assert.Contains("ProductSortFragments.NameAsc", source);
    }

    [Fact]
    public void ProductRepositoryImpl_emits_paging_sql_properties()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("protected override string SelectAllPageSql", source);
        Assert.Contains("protected override string SelectAllSliceSql", source);
        Assert.Contains("protected override string CountPageSql", source);
        Assert.Contains("OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY", source);
        Assert.Contains("FETCH NEXT @sliceSize ROWS ONLY", source);
    }

    [Fact]
    public void ProductRepositoryImpl_SelectAllPageSql_sqlserver_has_order_by_before_offset()
    {
        var source = ReadGeneratedProductRepository();
        var pageSql = ExtractSqlConstant(source, "SelectAllPageSql");
        AssertSqlServerOrderByBeforeOffset(pageSql);
    }

    [Fact]
    public void ProductRepositoryImpl_SelectAllSliceSql_sqlserver_has_order_by_before_offset()
    {
        var source = ReadGeneratedProductRepository();
        var sliceSql = ExtractSqlConstant(source, "SelectAllSliceSql");
        AssertSqlServerOrderByBeforeOffset(sliceSql);
    }

    private static string ExtractSqlConstant(string source, string constantName)
    {
        var marker = $"protected override string {constantName} => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {constantName}");
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        return source.Substring(start, end - start);
    }

    private static void AssertSqlServerOrderByBeforeOffset(string sql)
    {
        var orderIndex = sql.IndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase);
        var offsetIndex = sql.IndexOf("OFFSET", StringComparison.OrdinalIgnoreCase);
        Assert.True(orderIndex >= 0, "Expected ORDER BY in paging SQL.");
        Assert.True(offsetIndex >= 0, "Expected OFFSET in paging SQL.");
        Assert.True(orderIndex < offsetIndex, "ORDER BY must precede OFFSET in SqlServer paging SQL.");
    }

    [Fact]
    public void ProductRepositoryImpl_GetAllSliceAsync_overloads_available()
    {
        var sliceMethods = typeof(ProductRepositoryImpl)
            .GetMethods()
            .Where(m => m.Name == nameof(IRepository<Product, int>.GetAllSliceAsync))
            .Select(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name)))
            .ToHashSet();

        Assert.Contains(sliceMethods, s => s.StartsWith("Pageable", StringComparison.Ordinal));
        Assert.Contains(sliceMethods, s => s.StartsWith("Sort,Pageable", StringComparison.Ordinal));
    }
}
