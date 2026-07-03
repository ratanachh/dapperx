namespace DapperX.Tests;

public class SoftDeleteBypassGenerationTests
{
    private static string ReadGenerated(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            fileName));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_emits_paired_IncludingDeleted_sql_constants()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("private const string SelectByIdSqlIncludingDeleted", source);
        Assert.Contains("private const string SelectAllSqlIncludingDeleted", source);
        Assert.Contains("private const string SelectByIdsSqlIncludingDeleted", source);
        Assert.Contains("private const string ExistsSqlIncludingDeleted", source);
        Assert.Contains("private const string CountSqlIncludingDeleted", source);
        Assert.Contains("private const string SelectAllPageSqlIncludingDeleted", source);
        Assert.Contains("private const string SelectAllSliceSqlIncludingDeleted", source);
        Assert.Contains("private const string CountPageSqlIncludingDeleted", source);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_SelectByIdSqlIncludingDeleted_omits_soft_delete_predicate()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        var bypass = ExtractPrivateConst(source, "SelectByIdSqlIncludingDeleted");
        Assert.DoesNotContain("is_deleted = 0", bypass);
        Assert.Contains("id = @Id", bypass);
        var active = ExtractProtectedOverride(source, "SelectByIdSql");
        Assert.Contains("is_deleted = 0", active);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_GetByIdAsync_selects_sql_by_includeDeleted()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        var methodStart = source.IndexOf("GetByIdAsync(int id, bool includeDeleted", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var body = source.Substring(methodStart, 600);
        Assert.Contains("includeDeleted ? SelectByIdSqlIncludingDeleted : SelectByIdSql", body);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_Cpql_includeDeleted_emits_paired_sql_constants()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        var marker = "FindByNameCpqlAsync(string name";
        var methodStart = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = source.IndexOf("    public ", methodStart + marker.Length, StringComparison.Ordinal);
        if (methodEnd < 0)
            methodEnd = source.Length;
        var body = source.Substring(methodStart, methodEnd - methodStart);
        Assert.Contains("const string sqlActiveOnly", body);
        Assert.Contains("const string sqlIncludingDeleted", body);
        Assert.Contains("includeDeleted ? sqlIncludingDeleted : sqlActiveOnly", body);
        Assert.Contains("is_deleted = 0", body);
        Assert.DoesNotContain("is_deleted = 0", ExtractCpqlIncludingDeleted(body));
    }

    private static string ExtractCpqlIncludingDeleted(string methodBody)
    {
        const string marker = "const string sqlIncludingDeleted = \"";
        var start = methodBody.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += marker.Length;
        var end = methodBody.IndexOf("\";", start, StringComparison.Ordinal);
        return methodBody.Substring(start, end - start);
    }

    private static string ExtractPrivateConst(string source, string constantName)
    {
        var marker = $"private const string {constantName} = \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {constantName} in generated source.");
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Unterminated {constantName} literal.");
        return source.Substring(start, end - start);
    }

    private static string ExtractProtectedOverride(string source, string constantName)
    {
        var marker = $"protected override string {constantName} => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing {constantName} in generated source.");
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        Assert.True(end > start, $"Unterminated {constantName} literal.");
        return source.Substring(start, end - start);
    }
}
