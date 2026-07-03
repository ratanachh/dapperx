namespace Dapper.Npa.Tests.Shared.Generation;

public class CpqlEmittedSqlMatrixTests
{
    [Fact]
    public void MatrixCpqlItem_emits_translated_cpql_literals()
    {
        var source = GeneratedSourceReader.Read("MatrixCpqlItemRepositoryImpl.g.cs");
        Assert.True(
            source.Contains("SUBSTRING", StringComparison.OrdinalIgnoreCase)
            || source.Contains("SUBSTR", StringComparison.OrdinalIgnoreCase),
            "Expected SUBSTRING/SUBSTR in CPQL SQL.");
        Assert.Contains("FindBySkuPrefixCpqlAsync", source);
    }
}

public class CpqlMutationMatrixTests
{
    [Fact]
    public void MatrixCpqlItem_emits_update_cpql_mutation()
    {
        var source = GeneratedSourceReader.Read("MatrixCpqlItemRepositoryImpl.g.cs");
        Assert.Contains("UpdateSkuByIdCpqlAsync", source);
        Assert.Contains("UPDATE matrix_cpql_items", source, StringComparison.OrdinalIgnoreCase);
    }
}

public class SequenceMatrixTests
{
    [Fact]
    public void MatrixNumberedItem_sequence_sql_matches_provider()
    {
#if DAPPERX_PROVIDER_SQLITE
        var source = ProviderExpectations.ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("DPX017", source);
        Assert.Contains("SequenceNotSupportedOnSqlite", source);
#else
        var source = GeneratedSourceReader.Read("MatrixNumberedItemRepositoryImpl.g.cs");
        ProviderExpectations.AssertSequenceSql(source);
#endif
    }
}

public class StoredProcedureMatrixTests
{
    [Fact]
    public void MatrixProcOrder_stored_procedure_call_matches_provider()
    {
#if DAPPERX_PROVIDER_SQLITE
        var source = ProviderExpectations.ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("StoredProcedureNotSupportedOnSqlite", source);
#else
        var source = GeneratedSourceReader.Read("MatrixProcOrderRepositoryImpl.g.cs");
        ProviderExpectations.AssertStoredProcedureCall(source);
        Assert.Contains("ListOrdersSpAsync", source);
#endif
    }
}

public class MethodNameLoggingMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_batch_and_graph_paths_emit_method_name_logging()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        Assert.Contains("CreateLogContext(MethodName", source);
        Assert.Contains("InsertManyAsync", source);
    }

    [Fact]
    public void MatrixGraphParent_graph_methods_emit_method_name()
    {
        var source = GeneratedSourceReader.Read("MatrixGraphParentRepositoryImpl.g.cs");
        Assert.Contains("const string MethodName = \"UpdateGraphAsync\"", source);
        Assert.Contains("CreateLogContext(MethodName", source);
    }
}

public class Section23ChecklistMatrixTests
{
    [Fact]
    public void MatrixCatalogItem_repository_impl_spot_checks_section_23_items()
    {
        var source = GeneratedSourceReader.Read("MatrixCatalogItemRepositoryImpl.g.cs");
        Assert.Contains("ResolveColumn", source);
        Assert.Contains("UpsertSql", source);
        Assert.Contains("SelectAllSliceSql", source);
        Assert.Contains("SelectAllPageSql", source);
        Assert.Contains("DeleteByIdsSql", source);
        Assert.Contains("const string MethodName", source);
        ProviderExpectations.AssertUpsertSql(GeneratedSourceReader.ExtractSqlConstant(source, "UpsertSql"));
    }
}
