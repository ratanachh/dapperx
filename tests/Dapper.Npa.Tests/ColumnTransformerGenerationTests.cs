namespace Dapper.Npa.Tests;

public class ColumnTransformerGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ColumnTransformerProductRepositoryImpl_applies_read_and_write_expressions()
    {
        var source = ReadGenerated("ColumnTransformerProductRepositoryImpl.g.cs");

        Assert.Contains("(name) AS DisplayName", source);
        Assert.Contains("(UPPER(name)) AS UpperName", source);
        Assert.Contains("name = @DisplayName", source);
        Assert.DoesNotContain("upper_name = @UpperName", source);
    }
}
