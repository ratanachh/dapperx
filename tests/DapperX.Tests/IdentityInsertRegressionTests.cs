namespace DapperX.Tests;

public class IdentityInsertRegressionTests
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

    private static string ExtractInsertSql(string source)
    {
        const string marker = "protected override string InsertSql => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        return source.Substring(start, end - start);
    }

    [Fact]
    public void ProductRepositoryImpl_InsertSql_omits_identity_id_column()
    {
        var insertSql = ExtractInsertSql(ReadGenerated("ProductRepositoryImpl.g.cs"));
        Assert.DoesNotContain("(id,", insertSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@Id", insertSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductRepositoryImpl_InsertManyAsync_uses_same_insert_sql_shape()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        var insertSql = ExtractInsertSql(source);
        var insertManyStart = source.IndexOf("public override async Task InsertManyAsync", StringComparison.Ordinal);
        Assert.True(insertManyStart >= 0);
        var body = source.Substring(insertManyStart);
        Assert.Contains("InsertSql", body);
        Assert.DoesNotContain("@Id", insertSql, StringComparison.OrdinalIgnoreCase);
    }
}
