namespace DapperX.Tests.Sqlite;

public class SqliteProviderGenerationTests
{
    [Fact]
    public void Generated_catalog_repository_uses_limit_slice_paging()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "CatalogItemRepositoryImpl.g.cs"));

        Assert.True(File.Exists(path), "Expected generated CatalogItemRepositoryImpl.g.cs");
        var source = File.ReadAllText(path);
        Assert.Contains("LIMIT @sliceSize", source);
        Assert.DoesNotContain("FETCH NEXT @sliceSize", source);
    }
}
