namespace DapperX.Tests;

public class ErrorHandlingGenerationTests
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
    public void ProductRepositoryImpl_routes_sql_through_DbExecutor()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.Contains("using DapperX.Runtime.Execution;", source);
        Assert.Contains("DbExecutor.QueryAsync", source);
        Assert.Contains("DbExecutor.ExecuteAsync(_connection,", source);
        Assert.Contains("DbExecutor.CreateLogContext(MethodName, Options, Provider)", source);
        Assert.DoesNotContain("await _connection.ExecuteAsync", source);
        Assert.DoesNotContain("await _connection.QueryAsync", source);
    }
}
