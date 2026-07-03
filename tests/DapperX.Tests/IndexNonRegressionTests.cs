namespace DapperX.Tests;

public class IndexNonRegressionTests
{
    [Fact]
    public void ProductRepositoryImpl_sql_unchanged_by_index_metadata()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("INSERT INTO products", source);
        Assert.DoesNotContain("CREATE INDEX", source);
        Assert.DoesNotContain("IndexMetadata", source);
    }
}
