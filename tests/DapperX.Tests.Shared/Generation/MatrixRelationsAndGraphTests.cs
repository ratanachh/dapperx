namespace Dapper.Npa.Tests.Shared.Generation;

public class LockingMatrixTests
{
    [Fact]
    public void MatrixLockedProduct_locking_matches_provider()
    {
        var source = GeneratedSourceReader.Read("MatrixLockedProductRepositoryImpl.g.cs");
#if DAPPERX_PROVIDER_SQLITE
        Assert.DoesNotContain("FindByNameLockedAsync", source);
#else
        Assert.Contains("FindByNameLockedAsync", source);
        if (ProviderExpectations.CurrentProvider == "SqlServer")
        {
            Assert.Contains("SqlServerTableHint.Apply(sql, lockHint)", source);
            Assert.Contains("HOLDLOCK", source, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            Assert.Contains("lockMode switch", source);
            Assert.Contains("FOR SHARE", source, StringComparison.OrdinalIgnoreCase);
        }
#endif
    }
}

public class LockTimeoutMatrixTests
{
    [Fact]
    public void Generator_defines_lock_timeout_diagnostics_and_branches()
    {
        var diagnostics = ProviderExpectations.ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("MySqlLockTimeoutUnsupported", diagnostics);
        var queryGen = ProviderExpectations.ReadGeneratorSource("Generators/QueryGenerator.cs");
        Assert.Contains("MySqlLockTimeoutUnsupported", queryGen);
    }
}

public class OptimisticConcurrencyMatrixTests
{
    [Fact]
    public void MatrixVersionedItem_update_and_delete_check_row_version()
    {
        var source = GeneratedSourceReader.Read("MatrixVersionedItemRepositoryImpl.g.cs");
        Assert.Contains("row_version = @RowVersion", GeneratedSourceReader.ExtractSqlConstant(source, "UpdateSql"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("row_version = @RowVersion", GeneratedSourceReader.ExtractSqlConstant(source, "DeleteSql"), StringComparison.OrdinalIgnoreCase);
    }
}

public class PessimisticWriteMatrixTests
{
    [Fact]
    public void DerivedQueryGenerator_emits_pessimistic_write_lock_literals()
    {
#if DAPPERX_PROVIDER_SQLITE
        var diagnostics = ProviderExpectations.ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("LockModeNotSupportedOnSqlite", diagnostics);
#else
        var source = ProviderExpectations.ReadGeneratorSource("Generators/DerivedQueryGenerator.cs");
        if (ProviderExpectations.CurrentProvider == "SqlServer")
            Assert.Contains("UPDLOCK", source, StringComparison.OrdinalIgnoreCase);
        else
            Assert.Contains("FOR UPDATE", source, StringComparison.OrdinalIgnoreCase);
#endif
    }
}

public class SecondaryTableMatrixTests
{
    [Fact]
    public void MatrixDocument_emits_secondary_table_join_and_ordered_mutations()
    {
        var source = GeneratedSourceReader.Read("MatrixDocumentRepositoryImpl.g.cs");
        Assert.Contains("matrix_document_details", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("InsertAsync", source);
        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        var deleteStart = source.IndexOf("public override async Task DeleteAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0 && deleteStart > insertStart);
    }
}

public class PrimaryKeyJoinColumnMatrixTests
{
    [Fact]
    public void MatrixUser_insert_assigns_profile_id_before_insert()
    {
        var source = GeneratedSourceReader.Read("MatrixUserRepositoryImpl.g.cs");
        Assert.Contains("Profile.Id = entity.Id", source);
        Assert.Contains("matrix_user_profiles", source, StringComparison.OrdinalIgnoreCase);
    }
}

public class LazyMapMatrixTests
{
    [Fact]
    public void MatrixDepartment_emits_load_map_for_many_sql()
    {
        var source = GeneratedSourceReader.Read("MatrixDepartmentRepositoryImpl.g.cs");
        Assert.Contains("LoadEmployeesByCodeForManySql", source);
        ProviderExpectations.AssertInClause(source, "department_id", "parentIds");
    }
}

public class ElementCollectionMatrixTests
{
    [Fact]
    public void MatrixGalleryProduct_emits_element_collection_sql()
    {
        var source = GeneratedSourceReader.Read("MatrixGalleryProductRepositoryImpl.g.cs");
        Assert.Contains("matrix_product_images", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LoadImages", source);
    }
}

public class NamedEntityGraphMatrixTests
{
    [Fact]
    public void MatrixGraphOrder_emits_named_graph_switch()
    {
        var source = GeneratedSourceReader.Read("MatrixGraphOrderRepositoryImpl.g.cs");
        Assert.Contains("matrixGraphOrder.withLines", source);
        ProviderExpectations.AssertBooleanFilterLiteral(source, "is_deleted", false);
    }
}

public class GraphExecutionMatrixTests
{
    [Fact]
    public void MatrixGraphParent_emits_graph_update_and_delete()
    {
        var source = GeneratedSourceReader.Read("MatrixGraphParentRepositoryImpl.g.cs");
        Assert.Contains("UpdateGraphAsync", source);
        Assert.Contains("DeleteGraphAsync", source);
        Assert.Contains("ChildrenRepo", source);
    }
}
