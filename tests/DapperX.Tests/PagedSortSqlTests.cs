using System.Reflection;
using DapperX.Runtime.Repositories;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class PagedSortSqlTests
{
    private static string ApplySort(string sql, string sort)
    {
        var method = typeof(DapperXRepositoryBase<Product, int>).GetMethod(
            "ApplySortToPagedSql",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [sql, sort])!;
    }

    private const string PageSql =
        "SELECT id FROM products ORDER BY id OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

    [Fact]
    public void ApplySortToPagedSql_replaces_default_order_by_before_offset()
    {
        var result = ApplySort(PageSql, " ORDER BY sku ASC");
        Assert.Contains(" ORDER BY sku ASC OFFSET ", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ORDER BY id", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplySortToPagedSql_without_sort_leaves_sql_unchanged()
    {
        Assert.Equal(PageSql, ApplySort(PageSql, ""));
    }

    [Fact]
    public void ApplySortToPagedSql_without_offset_appends_sort_at_end()
    {
        const string sql = "SELECT id FROM products";
        const string sort = " ORDER BY sku ASC";
        Assert.Equal(sql + sort, ApplySort(sql, sort));
    }
}
