namespace DapperX.Tests;

public class GlobalFilterGenerationTests
{
    [Fact]
    public void CatalogItemRepositoryImpl_emits_filter_constants_and_ApplyGlobalFilters()
    {
        var source = ReadGenerated("CatalogItemRepositoryImpl.g.cs");
        Assert.Contains("private static readonly string FILTER_ActiveOnly", source);
        Assert.Contains("ApplyGlobalFilters", source);
        Assert.Contains("IDapperXOptions", source);
        Assert.Contains("IsFilterActive(\"ActiveOnly\")", source);
        Assert.Contains("sql += FILTER_ActiveOnly", source);
    }

    [Fact]
    public void CatalogItemRepositoryImpl_derived_query_applies_global_filters()
    {
        var source = ReadGenerated("CatalogItemRepositoryImpl.g.cs");
        Assert.Contains("FindByNameAsync", source);
        Assert.Contains("sql = ApplyGlobalFilters(sql)", source);
    }

    [Fact]
    public void CatalogItemRepositoryImpl_mutating_applies_global_filters()
    {
        var source = ReadGenerated("CatalogItemRepositoryImpl.g.cs");
        Assert.Contains("ApplyGlobalFilters(UpdateSql)", source);
        Assert.Contains("ApplyGlobalFilters(DeleteSql)", source);
    }

    [Fact]
    public void FilteredSuperItemRepositoryImpl_inherits_mapped_superclass_filter()
    {
        var source = ReadGenerated("FilteredSuperItemRepositoryImpl.g.cs");
        Assert.Contains("FILTER_FromBase", source);
        Assert.Contains("status = 'active'", source);
        Assert.Contains("ApplyGlobalFilters", source);
    }

    [Fact]
    public void TenantRegionUserRepositoryImpl_emits_BuildReadParameters()
    {
        var source = ReadGenerated("TenantRegionUserRepositoryImpl.g.cs");
        Assert.Contains("private Dapper.DynamicParameters BuildReadParameters(object? extra = null)", source);
        Assert.Contains("GetFilterParameters(\"active_region\")", source);
        Assert.Contains("AddDynamicParams(__filterParams)", source);
    }

    [Fact]
    public void TenantRegionUserRepositoryImpl_GetAllAsync_uses_BuildReadParameters()
    {
        var source = ReadGenerated("TenantRegionUserRepositoryImpl.g.cs");
        var getAllStart = source.IndexOf("public override async Task<IEnumerable<DapperX.Tests.Fixtures.TenantRegionUser>> GetAllAsync(", StringComparison.Ordinal);
        Assert.True(getAllStart >= 0);
        var body = source.Substring(getAllStart, Math.Min(800, source.Length - getAllStart));
        Assert.Contains("BuildReadParameters()", body);
        Assert.DoesNotContain("new { tenantId = _tenantProvider", body);
    }

    [Fact]
    public void TenantRegionUserRepositoryImpl_DeleteByIdAsync_uses_BuildReadParameters_with_id()
    {
        var source = ReadGenerated("TenantRegionUserRepositoryImpl.g.cs");
        var deleteStart = source.IndexOf("public override async Task DeleteByIdAsync(int id", StringComparison.Ordinal);
        Assert.True(deleteStart >= 0);
        var body = source.Substring(deleteStart, Math.Min(600, source.Length - deleteStart));
        Assert.Contains("BuildReadParameters(new { Id = id })", body);
    }

    private static string ReadGenerated(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            fileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }
}
