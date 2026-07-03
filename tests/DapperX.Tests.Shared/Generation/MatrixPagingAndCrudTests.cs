namespace DapperX.Tests.Shared.Generation;

public class PagePagingMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_select_all_page_sql_matches_provider()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var pageSql = GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllPageSql");
        ProviderExpectations.AssertPagePaging(pageSql);
        Assert.Contains("COUNT", GeneratedSourceReader.ExtractSqlConstant(source, "CountPageSql"), StringComparison.OrdinalIgnoreCase);
        if (ProviderExpectations.CurrentProvider == "SqlServer")
            ProviderExpectations.AssertSqlServerOrderByBeforeOffset(pageSql);
    }
}

public class GeneratedColumnMatrixTests
{
    [Fact]
    public void MatrixGeneratedOrder_insert_uses_provider_identity_path()
    {
        var source = GeneratedSourceReader.Read("MatrixGeneratedOrderRepositoryImpl.g.cs");
        var insertSql = GeneratedSourceReader.ExtractSqlConstant(source, "InsertSql");
        ProviderExpectations.AssertIdentityInsert(insertSql);
        Assert.DoesNotContain("created_at", GeneratedSourceReader.ExtractSqlConstant(source, "UpdateSql"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GeneratedColumnsReSelectSql", source);
    }
}

public class BulkInsertMatrixTests
{
    [Fact]
    public void MatrixBulkShipment_insert_many_uses_provider_bulk_path()
    {
        var source = GeneratedSourceReader.Read("MatrixBulkShipmentRepositoryImpl.g.cs");
        ProviderExpectations.AssertBulkInsertPath(source);
    }
}

public class DeleteAllByIdMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_delete_by_ids_uses_in_clause()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var sql = GeneratedSourceReader.ExtractSqlConstant(source, "DeleteByIdsSql");
        ProviderExpectations.AssertInClause(sql, "id", "ids");
    }
}

public class GeneratedValueMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_insert_uses_provider_identity()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var insertSql = GeneratedSourceReader.ExtractSqlConstant(source, "InsertSql");
        ProviderExpectations.AssertIdentityInsert(insertSql);
        ProviderExpectations.AssertIdentityInsertExcludesId(insertSql);
    }
}

public class GetAllOverloadMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_emits_sort_fragments_and_paging_sql()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        Assert.Contains("MatrixCatalogItemSortFragments", source);
        Assert.Contains("GetSortFragment", source);
        Assert.Contains("SelectAllPageSql", source);
        Assert.Contains("SelectAllSliceSql", source);
    }
}

public class DerivedQueryPagingMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_derived_paged_query_uses_provider_limit_offset()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        Assert.Contains("FindBySkuPagedAsync", source);
        if (ProviderExpectations.CurrentProvider == "SqlServer")
            Assert.Contains("OFFSET", source, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Contains("LIMIT @pageSize", source, StringComparison.OrdinalIgnoreCase);
    }
}
