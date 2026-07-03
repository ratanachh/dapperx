using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class RepositoryBatchGenerationTests
{
    [Fact]
    public void ProductRepositoryImpl_overrides_InsertManyAsync()
    {
        var method = typeof(ProductRepositoryImpl)
            .GetMethods()
            .FirstOrDefault(m => m.Name == "InsertManyAsync" && m.DeclaringType == typeof(ProductRepositoryImpl));
        Assert.NotNull(method);
    }

    [Fact]
    public void Generated_ProductRepositoryImpl_declares_batch_methods()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.Contains("InsertManyAsync", source);
        Assert.Contains("UpdateManyAsync", source);
        Assert.Contains("DeleteManyAsync", source);
        Assert.Contains("BatchChunker.Chunk", source);
        Assert.Contains("effectiveBatchSize = batchSize ?? _options?.BatchSize ?? 1000", source);
        Assert.Contains("BatchChunker.Chunk(list, effectiveBatchSize)", source);
        Assert.Contains("DbExecutor.ExecuteAsync(_connection,", source);
        Assert.Contains("ExecuteAsync(_connection,", source);
        Assert.Contains("UpdateSql, chunk", source);
        Assert.Contains("DeleteSql, chunk", source);
        Assert.Contains("var conflictingKeys = new List<object>();", source);
        Assert.Contains("if (totalAffected != totalAttempted)", source);
        Assert.Contains("ConcurrencyException(\"One or more updates failed due to optimistic concurrency conflicts.\", conflictingKeys)", source);
        Assert.Contains("ConcurrencyException(\"One or more deletes failed due to optimistic concurrency conflicts.\", conflictingKeys)", source);
    }
}
