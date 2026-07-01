namespace DapperX.Tests;

public class DeleteAllByIdGenerationTests
{
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

    [Fact]
    public void ProductRepositoryImpl_DeleteByIdsSql_uses_in_clause()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.Contains("protected override string DeleteByIdsSql => \"DELETE FROM products WHERE id IN @ids\"", source);
    }

    [Fact]
    public void BatchLifecycleItemRepositoryImpl_DeleteAllById_invokes_batch_hooks_not_per_entity()
    {
        var source = ReadGenerated("BatchLifecycleItemRepositoryImpl.g.cs");
        var start = source.IndexOf("public override async Task DeleteAllByIdAsync", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = source.IndexOf("public override async Task InsertAsync", start, StringComparison.Ordinal);
        var body = source[start..end];
        Assert.Contains("InvokePreRemoveBatch(empty)", body);
        Assert.Contains("InvokePostRemoveBatch(empty)", body);
        Assert.DoesNotContain("OnPreRemove(entity)", body);
        Assert.DoesNotContain("foreach (var entity", body);
    }

    [Fact]
    public void CompositeKeyGenerator_validates_DeleteAllById_on_interface()
    {
        var source = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DapperX.Generator", "Generators", "CompositeKeyGenerator.cs")));
        Assert.Contains("DeleteAllByIdAsync", source);
        Assert.Contains("CompositeKeyBulkIdMethod", source);
    }
}
