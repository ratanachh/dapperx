namespace Dapper.Npa.Tests.Sqlite;

/// <summary>Sqlite-only: compile-time Diagnostic contracts (generator source) and positive Sqlite codegen paths.</summary>
public class SqliteUnsupportedFeatureTests
{
    private static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Dapper.Npa.Generator", relativePath));
        return File.ReadAllText(path);
    }

    [Fact]
    public void DiagnosticsReporter_defines_sqlite_lock_sequence_sp_and_regex_diagnostics()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("DPX037", source);
        Assert.Contains("LockModeNotSupportedOnSqlite", source);
        Assert.Contains("DPX038", source);
        Assert.Contains("SqliteQueryLockNotSupported", source);
        Assert.Contains("DPX017", source);
        Assert.Contains("SequenceNotSupportedOnSqlite", source);
        Assert.Contains("StoredProcedureNotSupportedOnSqlite", source);
        Assert.Contains("DPX029", source);
        Assert.Contains("RegexWarningOnSqlite", source);
        Assert.Contains("DPX036", source);
        Assert.Contains("MultipleResultSetsNotSupportedOnSqlite", source);
    }

    [Fact]
    public void DerivedQueryGenerator_reports_DPX037_for_sqlite_lock_mode_parameter()
    {
        var source = ReadGeneratorSource("Generators/DerivedQueryGenerator.cs");
        Assert.Contains("LockModeNotSupportedOnSqlite", source);
        Assert.Contains("provider != \"Sqlite\"", source);
    }

    [Fact]
    public void MatrixBulkShipment_insert_many_always_uses_batch_chunker_not_bulk_copy()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            "MatrixBulkShipmentRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("SqlBulkCopy", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BatchChunker.Chunk", source);
    }

    [Fact]
    public void SchemaNotSupportedOnSqlite_diagnostic_is_defined()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("SchemaNotSupportedOnSqlite", source);
    }
}
