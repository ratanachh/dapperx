namespace Dapper.Npa.Tests;

public class BulkInsertGenerationTests
{
    private static string ReadGenerated(string implFileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            implFileName));
        Assert.True(File.Exists(path), $"Expected generated file at {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void BulkShipmentRepositoryImpl_emits_bulk_metadata_and_threshold_branch()
    {
        var source = ReadGenerated("BulkShipmentRepositoryImpl.g.cs");

        Assert.Contains("BulkInsertTableName", source);
        Assert.Contains("bulk_shipments", source);
        Assert.Contains("BulkInsertColumnNames", source);
        Assert.Contains("BuildBulkInsertRow", source);
        Assert.Contains("effectiveBulkThreshold = bulkThreshold ?? _options?.BulkThreshold ?? 5000", source);
        Assert.Contains("list.Count >= effectiveBulkThreshold", source);
        Assert.Contains("SqlServerBulkExecutor", source);
        Assert.Contains("BulkInsertContext", source);
    }

    [Fact]
    public void BulkShipmentRepositoryImpl_falls_back_to_batch_insert_below_threshold()
    {
        var source = ReadGenerated("BulkShipmentRepositoryImpl.g.cs");
        var insertStart = source.IndexOf("public override async Task InsertManyAsync", StringComparison.Ordinal);
        Assert.True(insertStart >= 0);
        var body = source.Substring(insertStart);

        Assert.Contains("BatchChunker.Chunk(list, effectiveBatchSize)", body);
        Assert.Contains("DbExecutor.ExecuteAsync(_connection, InsertSql, chunk, transaction", body);
        Assert.Contains("CreateLogContext(MethodName, Options, Provider)", body);
    }

    [Fact]
    public void BulkShipmentRepositoryImpl_uses_options_batch_size()
    {
        var source = ReadGenerated("BulkShipmentRepositoryImpl.g.cs");
        Assert.Contains("effectiveBatchSize = batchSize ?? _options?.BatchSize ?? 1000", source);
    }

    [Fact]
    public void ProductRepositoryImpl_does_not_emit_bulk_insert_path()
    {
        var source = ReadGenerated("ProductRepositoryImpl.g.cs");
        Assert.DoesNotContain("BulkInsertTableName", source);
        Assert.DoesNotContain("SqlServerBulkExecutor", source);
    }

    [Fact]
    public void DatabaseProviderFactory_supports_bulk_insert_for_sql_server_postgresql_mysql_only()
    {
        Assert.True(Dapper.Npa.Provider.Common.DatabaseProviderFactory.SupportsBulkInsert("SqlServer"));
        Assert.True(Dapper.Npa.Provider.Common.DatabaseProviderFactory.SupportsBulkInsert("PostgreSql"));
        Assert.True(Dapper.Npa.Provider.Common.DatabaseProviderFactory.SupportsBulkInsert("MySql"));
        Assert.False(Dapper.Npa.Provider.Common.DatabaseProviderFactory.SupportsBulkInsert("Sqlite"));
    }

    [Fact]
    public void SqlServerProvider_exposes_bulk_executor()
    {
        var provider = new Dapper.Npa.Provider.SqlServer.SqlServerProvider();
        Assert.True(provider.SupportsBulkInsert);
        Assert.NotNull(provider.BulkInsertExecutor);
    }

    [Fact]
    public void SqliteProvider_returns_null_bulk_executor()
    {
        var provider = new Dapper.Npa.Provider.Sqlite.SqliteProvider();
        Assert.False(provider.SupportsBulkInsert);
        Assert.Null(provider.BulkInsertExecutor);
    }
}
