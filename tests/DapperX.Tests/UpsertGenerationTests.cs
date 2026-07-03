namespace DapperX.Tests;

public class UpsertGenerationTests
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
    public void ProductRepositoryImpl_emits_upsert_sql_and_methods()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");

        Assert.Contains("protected override string UpsertSql", source);
        Assert.Contains("MERGE", source);
        Assert.Contains("public override async Task UpsertAsync", source);
        Assert.Contains("public override async Task UpsertManyAsync", source);
        Assert.Contains("DbExecutor.ExecuteAsync(_connection, UpsertSql", source);
        Assert.Contains("const string MethodName = \"UpsertAsync\"", source);
        Assert.Contains("const string MethodName = \"UpsertManyAsync\"", source);
    }

    [Fact]
    public void ProductRepositoryImpl_upsert_does_not_invoke_lifecycle_hooks()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        var upsertStart = source.IndexOf("public override async Task UpsertAsync", StringComparison.Ordinal);
        var upsertEnd = source.IndexOf("public override async Task UpsertManyAsync", StringComparison.Ordinal);
        Assert.True(upsertStart >= 0 && upsertEnd > upsertStart);
        var upsertBody = source.Substring(upsertStart, upsertEnd - upsertStart);
        Assert.DoesNotContain("OnPrePersist", upsertBody);
        Assert.DoesNotContain("OnPostPersist", upsertBody);
        Assert.DoesNotContain("OnPreUpdate", upsertBody);
    }
}
