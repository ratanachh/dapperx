using System.Reflection;
using DapperX.Generator.Builders;

namespace DapperX.Tests;

public class MultiDatabaseProviderTests
{
    private static string InvokeAppendSlicePaging(string baseSql, string provider)
    {
        var method = typeof(SqlBuilder).GetMethod(
            "AppendSlicePaging",
            BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return (string)method!.Invoke(null, [baseSql, provider, null])!;
    }

    [Theory]
    [InlineData("SqlServer", "FETCH NEXT @sliceSize")]
    [InlineData("PostgreSql", "LIMIT @sliceSize")]
    [InlineData("MySql", "LIMIT @sliceSize")]
    [InlineData("Sqlite", "LIMIT @sliceSize")]
    public void AppendSlicePaging_is_provider_specific(string provider, string expectedFragment)
    {
        var sql = InvokeAppendSlicePaging("SELECT id FROM products", provider);
        Assert.Contains(expectedFragment, sql, StringComparison.Ordinal);
        if (provider == "SqlServer")
            Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
    }

}
