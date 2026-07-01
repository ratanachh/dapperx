namespace DapperX.Tests;

public class SecondaryTableTests
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
    public void DocumentRepositoryImpl_uses_qualified_secondary_table_join()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("LEFT JOIN document_details st_document_details", source);
        Assert.Contains("ON st_document_details.document_id = e.id", source);
        Assert.DoesNotContain("document_id = id", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_uses_qualified_primary_columns_in_select()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("SELECT e.id AS Id, e.title AS Title, st_document_details.summary AS Summary", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_uses_qualified_id_in_select_by_id()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("WHERE e.id = @Id", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_emits_secondary_sql_literals()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("SecondaryInsert_document_details", source);
        Assert.Contains("INSERT INTO document_details (document_id, summary)", source);
        Assert.Contains("SecondaryUpdate_document_details", source);
        Assert.Contains("UPDATE document_details SET summary = @Summary WHERE document_id = @Id", source);
        Assert.Contains("SecondaryDelete_document_details", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_insert_primary_then_secondary()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var insertBody = source[insertStart..(insertStart + 600)];
        Assert.Contains("DbExecutor.ExecuteScalarAsync<int>(_connection, InsertSql", insertBody);
        Assert.Contains("SecondaryInsert_document_details", insertBody);
        Assert.True(insertBody.IndexOf("InsertSql", StringComparison.Ordinal)
            < insertBody.IndexOf("SecondaryInsert_document_details", StringComparison.Ordinal));
    }

    [Fact]
    public void DocumentRepositoryImpl_delete_secondary_before_primary()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        var deleteStart = source.IndexOf("public override async Task DeleteAsync", StringComparison.Ordinal);
        Assert.True(deleteStart >= 0);
        var deleteEnd = source.IndexOf("public override async Task DeleteByIdAsync", deleteStart, StringComparison.Ordinal);
        Assert.True(deleteEnd > deleteStart);
        var deleteBody = source[deleteStart..deleteEnd];
        Assert.True(deleteBody.IndexOf("SecondaryDelete_document_details", StringComparison.Ordinal)
            < deleteBody.IndexOf("DeleteSql", StringComparison.Ordinal));
    }

    [Fact]
    public void DocumentRepositoryImpl_wraps_mutating_in_transaction_when_unscoped()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("ownsTransaction = transaction is null", source);
        Assert.Contains("transaction ??= _connection.BeginTransaction()", source);
        Assert.Contains("transaction.Commit()", source);
        Assert.Contains("transaction.Rollback()", source);
    }

    [Fact]
    public void DocumentRepositoryImpl_emits_delete_all_by_id_secondary_first()
    {
        var source = ReadGenerated("DocumentRepositoryImpl.g.cs");
        Assert.Contains("SecondaryDeleteByIds_document_details", source);
    }
}
