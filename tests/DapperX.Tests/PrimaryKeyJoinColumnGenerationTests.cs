namespace DapperX.Tests;

public class PrimaryKeyJoinColumnGenerationTests
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

    [Fact]
    public void UserRepositoryImpl_emits_shared_pk_join_literal()
    {
        var source = ReadGenerated("UserRepositoryImpl.g.cs");
        Assert.Contains("QueryIncludeJoinSql", source);
        Assert.Contains("INNER JOIN user_profiles nav_Profile ON e.id = nav_Profile.id", source);
    }

    [Fact]
    public void UserRepositoryImpl_assigns_child_id_after_parent_identity_insert()
    {
        var source = ReadGenerated("UserRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertEnd = source.IndexOf("public override async Task UpsertAsync", insertStart, StringComparison.Ordinal);
        Assert.True(insertEnd > insertStart);
        var insertBody = source[insertStart..insertEnd];

        Assert.Contains("DbExecutor.ExecuteScalarAsync<int>(_connection, InsertSql", insertBody);
        Assert.Contains("entity.Id = newId", insertBody);
        Assert.Contains("entity.Profile.Id = entity.Id", insertBody);
        Assert.True(
            insertBody.IndexOf("entity.Id = newId", StringComparison.Ordinal)
            < insertBody.IndexOf("entity.Profile.Id = entity.Id", StringComparison.Ordinal),
            "Child Id must be assigned after parent Id is generated");
    }

    [Fact]
    public void UserRepositoryImpl_uses_property_assignment_not_sql_for_child_id()
    {
        var source = ReadGenerated("UserRepositoryImpl.g.cs");
        Assert.Contains("entity.Profile.Id = entity.Id", source);
        Assert.DoesNotContain("Profile.Id = \"", source);
        Assert.DoesNotContain("CONCAT", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserRepositoryImpl_uses_identity_insert_with_scope_identity()
    {
        var source = ReadGenerated("UserRepositoryImpl.g.cs");
        Assert.Contains("SCOPE_IDENTITY()", source);
        Assert.Contains("DbExecutor.ExecuteScalarAsync<int>(_connection, InsertSql", source);
    }
}
