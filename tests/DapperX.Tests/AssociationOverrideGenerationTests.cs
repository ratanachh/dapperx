namespace DapperX.Tests;

public class AssociationOverrideGenerationTests
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
    public void AdminDocumentRepositoryImpl_query_include_uses_overridden_owner_fk()
    {
        var source = ReadGenerated("AdminDocumentRepositoryImpl.g.cs");

        Assert.Contains("admin_user_id", source);
        Assert.Contains("nav_Owner", source);
    }
}
