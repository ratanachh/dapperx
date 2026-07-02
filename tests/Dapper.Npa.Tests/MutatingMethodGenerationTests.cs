namespace Dapper.Npa.Tests;

public class MutatingMethodGenerationTests
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
    public void ProductRepositoryImpl_InsertAsync_uses_ExecuteScalar_for_identity()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.Contains("DbExecutor.ExecuteScalarAsync<int>(_connection, InsertSql", source);
    }

    [Fact]
    public void AuditedProductRepositoryImpl_sql_includes_audit_columns_with_dialect_timestamps()
    {
        var source = ReadGenerated("AuditedProductRepositoryImpl.g.cs");
        Assert.Contains("created_at", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modified_at", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GETDATE()", source);
        Assert.DoesNotContain("created_at = @CreatedAt", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_applies_tenant_on_insert()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        Assert.Contains("ApplyTenantIdFromProvider", source);
        Assert.Contains("tenant_id", source, StringComparison.OrdinalIgnoreCase);
        var updateSql = ExtractUpdateSql(source);
        var setClause = updateSql.Split(" WHERE ", 2, StringSplitOptions.None)[0];
        Assert.DoesNotContain("tenant_id = @TenantId", setClause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tenant_id = @tenantId", updateSql, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractUpdateSql(string source)
    {
        const string marker = "protected override string UpdateSql => \"";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0);
        start += marker.Length;
        var end = source.IndexOf("\";", start, StringComparison.Ordinal);
        return source.Substring(start, end - start);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_DeleteSql_is_soft_update()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("is_deleted = 1", source);
        Assert.DoesNotContain("WHERE is_deleted = 0 WHERE", source);
    }
}
