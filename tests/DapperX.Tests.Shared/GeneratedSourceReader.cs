namespace Dapper.Npa.Tests.Shared;

/// <summary>Reads emitted <c>*RepositoryImpl.g.cs</c> files from the test project's generator output folder.</summary>
public static class GeneratedSourceReader
{
    public static string Read(string implFileName)
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

    public static string ExtractSqlConstant(string source, string constantName)
    {
        var marker = $"protected override string {constantName} => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected {constantName} in generated source.");
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        return source.Substring(start, end - start);
    }
}
