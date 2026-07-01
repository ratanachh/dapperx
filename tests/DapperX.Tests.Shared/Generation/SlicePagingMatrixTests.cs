namespace DapperX.Tests.Shared.Generation;

public class SlicePagingMatrixTests
{
    [Fact]
    public void MatrixCatalogItemRepositoryImpl_select_all_slice_sql_matches_provider()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var sliceSql = GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllSliceSql");
        ProviderExpectations.AssertSlicePaging(sliceSql);
        Assert.Contains("@sliceSize", sliceSql);
        if (ProviderExpectations.CurrentProvider == "SqlServer")
            ProviderExpectations.AssertSqlServerOrderByBeforeOffset(sliceSql);
    }
}
