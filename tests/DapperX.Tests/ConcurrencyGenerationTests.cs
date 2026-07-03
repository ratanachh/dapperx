namespace DapperX.Tests;

public class ConcurrencyGenerationTests
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
    public void VersionedGraphOrderRepositoryImpl_update_graph_rolls_back_on_failure()
    {
        var source = ReadGenerated("VersionedGraphOrderRepositoryImpl.g.cs");
        var updateStart = source.IndexOf("public override async Task UpdateGraphAsync", StringComparison.Ordinal);
        Assert.True(updateStart >= 0);
        var updateEnd = source.IndexOf("public override async Task DeleteGraphAsync", updateStart, StringComparison.Ordinal);
        var body = source.Substring(updateStart, updateEnd - updateStart);
        Assert.Contains("transaction.Rollback()", body);
        Assert.Contains("LinesRepo.UpdateManyAsync", body);
        Assert.Contains("ConcurrencyException", source);
    }

    [Fact]
    public void OrderRepositoryImpl_update_graph_rolls_back_on_failure()
    {
        var source = ReadGenerated("OrderRepositoryImpl.g.cs");
        var updateStart = source.IndexOf("public override async Task UpdateGraphAsync", StringComparison.Ordinal);
        var updateEnd = source.IndexOf("public override async Task DeleteGraphAsync", updateStart, StringComparison.Ordinal);
        var body = source.Substring(updateStart, updateEnd - updateStart);
        Assert.Contains("if (ownsTransaction) transaction.Rollback();", body);
    }
}
