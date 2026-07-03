namespace DapperX.Tests.Shared.Generation;

public class UpsertGenerationMatrixTests
{
    [Fact]
    public void MatrixCatalogItemRepositoryImpl_upsert_uses_provider_syntax()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var upsertSql = GeneratedSourceReader.ExtractSqlConstant(source, "UpsertSql");
        ProviderExpectations.AssertUpsertSql(upsertSql);
    }

    [Fact]
    public void MatrixCatalogItemRepositoryImpl_slice_uses_provider_paging()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        var sliceSql = GeneratedSourceReader.ExtractSqlConstant(source, "SelectAllSliceSql");
        ProviderExpectations.AssertSlicePaging(sliceSql);
    }
}
