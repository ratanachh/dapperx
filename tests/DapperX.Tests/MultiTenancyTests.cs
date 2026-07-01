namespace DapperX.Tests;

public class MultiTenancyTests
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
    public void TenantScopedItemRepositoryImpl_SelectByIdSql_includes_tenant_filter()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        var selectByIdSql = ExtractSqlConstant(source, "SelectByIdSql");
        Assert.Contains("tenant_id = @tenantId", selectByIdSql);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_UpdateSql_excludes_tenant_from_set_and_filters_where()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        var updateSql = ExtractSqlConstant(source, "UpdateSql");
        var setClause = updateSql.Split(" WHERE ", 2, StringSplitOptions.None)[0];
        Assert.DoesNotContain("tenant_id = @TenantId", setClause, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tenant_id = @tenantId", setClause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE id = @Id AND tenant_id = @tenantId", updateSql);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_DeleteSql_includes_tenant_in_where()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        Assert.Contains("WHERE id = @Id AND tenant_id = @tenantId", ExtractSqlConstant(source, "DeleteSql"));
        Assert.Contains("WHERE id = @Id AND tenant_id = @tenantId", ExtractSqlConstant(source, "DeleteByIdSql"));
        Assert.Contains("WHERE id IN @ids AND tenant_id = @tenantId", ExtractSqlConstant(source, "DeleteByIdsSql"));
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_GetByIdAsync_passes_tenantId_param()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        Assert.Contains("public override async Task<DapperX.Tests.Fixtures.TenantScopedItem?> GetByIdAsync", source);
        Assert.Contains("new { Id = id, tenantId = _tenantProvider?.GetCurrentTenantId() }", source);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_injects_ITenantProvider()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        Assert.Contains("ITenantProvider? tenantProvider", source);
        Assert.Contains("_tenantProvider = tenantProvider", source);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_InsertAsync_applies_tenant_before_insert()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var updateStart = source.IndexOf("public override async Task UpdateAsync", StringComparison.Ordinal);
        Assert.True(updateStart > insertStart);
        var insertBody = source.Substring(insertStart, updateStart - insertStart);
        Assert.Contains("ApplyTenantIdFromProvider(entity)", insertBody);
    }

    [Fact]
    public void TenantScopedItemRepositoryImpl_UpdateAsync_does_not_apply_tenant_on_update()
    {
        var source = ReadGenerated("TenantScopedItemRepositoryImpl.g.cs");
        var updateStart = source.IndexOf("public override async Task UpdateAsync", StringComparison.Ordinal);
        Assert.True(updateStart >= 0);
        var deleteStart = source.IndexOf("public override async Task DeleteAsync", StringComparison.Ordinal);
        Assert.True(deleteStart > updateStart);
        var updateBody = source.Substring(updateStart, deleteStart - updateStart);
        Assert.DoesNotContain("ApplyTenantIdFromProvider(entity)", updateBody);
        Assert.Contains("WithTenantParams(entity)", updateBody);
    }

    [Fact]
    public void MappedTenantItemRepositoryImpl_inherits_tenant_from_mapped_superclass()
    {
        var source = ReadGenerated("MappedTenantItemRepositoryImpl.g.cs");
        Assert.Contains("tenant_id = @tenantId", ExtractSqlConstant(source, "SelectByIdSql"));
        Assert.Contains("WHERE id = @Id AND tenant_id = @tenantId", ExtractSqlConstant(source, "UpdateSql"));
    }

    private static string ExtractSqlConstant(string source, string constantName)
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
