namespace DapperX.Tests;

public class SoftDeleteTests
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
    public void ArchivedItemRepositoryImpl_SelectByIdSql_uses_single_where_with_soft_delete_filter()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        var selectByIdSql = ExtractSqlConstant(source, "SelectByIdSql");
        Assert.Contains("is_deleted = 0", selectByIdSql);
        Assert.Contains("id = @Id", selectByIdSql);
        Assert.DoesNotContain("WHERE is_deleted = 0 WHERE", selectByIdSql);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_SelectByIdsSql_and_ExistsSql_include_soft_delete_filter()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        var selectByIdsSql = ExtractSqlConstant(source, "SelectByIdsSql");
        var existsSql = ExtractSqlConstant(source, "ExistsSql");
        Assert.Contains("is_deleted = 0", selectByIdsSql);
        Assert.DoesNotContain("WHERE is_deleted = 0 WHERE", selectByIdsSql);
        Assert.Contains("is_deleted = 0", existsSql);
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_delete_sql_rewrites_to_soft_update()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("UPDATE archived_items SET is_deleted = 1", ExtractSqlConstant(source, "DeleteSql"));
        Assert.Contains("UPDATE archived_items SET is_deleted = 1", ExtractSqlConstant(source, "DeleteByIdSql"));
        Assert.Contains("UPDATE archived_items SET is_deleted = 1", ExtractSqlConstant(source, "DeleteByIdsSql"));
    }

    [Fact]
    public void ArchivedItemRepositoryImpl_emits_HardDeleteAsync_with_true_delete()
    {
        var source = ReadGenerated("ArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("HardDeleteAsync", source);
        Assert.Contains("DELETE FROM archived_items WHERE id = @Id", source);
        Assert.Contains("DbExecutor.ExecuteAsync(_connection, HardDeleteSql, entity, transaction", source);
        Assert.Contains("CreateLogContext(MethodName, Options, Provider)", source);
    }

    [Fact]
    public void MappedArchivedItemRepositoryImpl_inherits_soft_delete_from_mapped_superclass()
    {
        var source = ReadGenerated("MappedArchivedItemRepositoryImpl.g.cs");
        Assert.Contains("is_deleted = 0", ExtractSqlConstant(source, "SelectByIdSql"));
        Assert.Contains("UPDATE mapped_archived_items SET is_deleted = 1", ExtractSqlConstant(source, "DeleteSql"));
        Assert.Contains("HardDeleteAsync", source);
    }

    [Fact]
    public void VersionedArchivedItemRepositoryImpl_soft_delete_includes_version_and_deleted_at()
    {
        var source = ReadGenerated("VersionedArchivedItemRepositoryImpl.g.cs");
        var deleteSql = ExtractSqlConstant(source, "DeleteSql");
        Assert.Contains("is_deleted = 1", deleteSql);
        Assert.Contains("deleted_at = GETDATE()", deleteSql);
        Assert.Contains("version = @Version", deleteSql);
        Assert.DoesNotContain("CURRENT_TIMESTAMP", deleteSql);
    }

    [Fact]
    public void SoftDeleteLifecycleItemRepositoryImpl_fires_remove_hooks_on_delete_by_id()
    {
        var source = ReadGenerated("SoftDeleteLifecycleItemRepositoryImpl.g.cs");
        var deleteByIdStart = source.IndexOf("public override async Task DeleteByIdAsync", StringComparison.Ordinal);
        Assert.True(deleteByIdStart >= 0);
        var deleteByIdEnd = source.IndexOf("public static string ResolveColumn", deleteByIdStart, StringComparison.Ordinal);
        var deleteByIdBody = deleteByIdEnd > deleteByIdStart
            ? source.Substring(deleteByIdStart, deleteByIdEnd - deleteByIdStart)
            : source.Substring(deleteByIdStart, 800);
        Assert.Contains("OnPreRemove(entity)", deleteByIdBody);
        Assert.Contains("OnPostRemove(entity)", deleteByIdBody);
        Assert.Contains("UPDATE soft_delete_lifecycle_items SET is_deleted = 1", ExtractSqlConstant(source, "DeleteByIdSql"));
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
