namespace Dapper.Npa.Tests.Shared.Generation;

public class AuditingSqlMatrixTests
{
    [Fact]
    public void MatrixAuditedProduct_sql_uses_provider_audit_timestamps()
    {
        var source = GeneratedSourceReader.Read("MatrixAuditedProductRepositoryImpl.g.cs");
        ProviderExpectations.AssertAuditingTimestamp(source);
        Assert.Contains("created_at", source, StringComparison.OrdinalIgnoreCase);
    }
}

public class SoftDeleteMatrixTests
{
    [Fact]
    public void MatrixArchivedItem_delete_rewrites_to_soft_delete_update()
    {
        var source = GeneratedSourceReader.Read("MatrixArchivedItemRepositoryImpl.g.cs");
        ProviderExpectations.AssertSoftDeleteDeleteSql(GeneratedSourceReader.ExtractSqlConstant(source, "DeleteSql"));
        ProviderExpectations.AssertBooleanFilterLiteral(
            GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllSql"), "is_deleted", false);
    }
}

public class TenancyMatrixTests
{
    [Fact]
    public void MatrixTenantItem_sql_includes_tenant_filter()
    {
        var source = GeneratedSourceReader.Read("MatrixTenantItemRepositoryImpl.g.cs");
        ProviderExpectations.AssertTenancyFilter(GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllSql"));
        ProviderExpectations.AssertTenancyFilter(GeneratedSourceReader.ExtractSqlConstant(source, "InsertSql"));
        Assert.Contains("ApplyTenantIdFromProvider", source);
    }
}

public class GlobalFilterMatrixTests
{
    [Fact]
    public void MatrixFilteredCatalog_emits_filter_constants()
    {
        var source = GeneratedSourceReader.Read("MatrixFilteredCatalogRepositoryImpl.g.cs");
        Assert.Contains("FILTER_ActiveOnly", source);
        Assert.Contains("ApplyGlobalFilters", source);
    }

    [Fact]
    public void MatrixTenantRegionUser_emits_BuildReadParameters_for_parameterized_filter()
    {
        var source = GeneratedSourceReader.Read("MatrixTenantRegionUserRepositoryImpl.g.cs");
        Assert.Contains("BuildReadParameters", source);
        Assert.Contains("GetFilterParameters(\"active_region\")", source);
        Assert.Contains("AddDynamicParams(__filterParams)", source);
        Assert.Contains("FILTER_Active_region", source);
    }
}

public class IncludeDeletedMatrixTests
{
    [Fact]
    public void MatrixArchivedItem_emits_paired_include_deleted_literals()
    {
        var source = GeneratedSourceReader.Read("MatrixArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("SelectAllSqlIncludingDeleted", source);
        Assert.Contains("SelectAllSql =>", source);
        ProviderExpectations.AssertBooleanFilterLiteral(
            GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllSql"), "is_deleted", false);
    }
}
