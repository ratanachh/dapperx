using System.Reflection;
using DapperX.Tests.Fixtures;

namespace DapperX.Tests;

public class LockingGenerationTests
{
    private static string ReadGeneratedProductRepository()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "DapperX.Generator", "DapperX.Generator.DapperXSourceGenerator",
            "ProductRepositoryImpl.g.cs"));
        return File.ReadAllText(path);
    }

    private static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DapperX.Generator", relativePath));
        return File.ReadAllText(path);
    }

    [Fact]
    public void ProductRepositoryImpl_implements_FindByNameLockedAsync()
    {
        var parameterTypes = new[] { typeof(string), typeof(DapperX.Core.Enums.LockMode) };
        Assert.NotNull(typeof(IProductRepository).GetMethod(nameof(IProductRepository.FindByNameLockedAsync), parameterTypes));
        Assert.NotNull(typeof(ProductRepositoryImpl).GetMethod(nameof(IProductRepository.FindByNameLockedAsync), parameterTypes));
    }

    [Fact]
    public void ProductRepositoryImpl_FindByNameLocked_uses_SqlServerTableHint_for_sqlserver()
    {
        var source = ReadGeneratedProductRepository();
        Assert.Contains("FindByNameLockedAsync(string name, DapperX.Core.Enums.LockMode lockMode)", source);
        Assert.Contains("DapperX.Runtime.Query.SqlServerTableHint.Apply(sql, lockHint)", source);
        Assert.Contains("WITH (UPDLOCK, ROWLOCK)", source);
        Assert.Contains("WITH (HOLDLOCK, ROWLOCK)", source);
        Assert.DoesNotContain("sql += lockMode switch", source);
    }

    [Fact]
    public void DerivedQueryGenerator_emits_postgresql_and_mysql_share_literals()
    {
        var source = ReadGeneratorSource("Generators/DerivedQueryGenerator.cs");
        Assert.Contains("\" FOR SHARE\"", source);
        Assert.Contains("MySql\" => \" FOR SHARE\"", source);
        Assert.DoesNotContain("LOCK IN SHARE MODE", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_DPX037_and_DPX038_sqlite_lock_diagnostics()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("\"DPX037\"", source);
        Assert.Contains("LockModeNotSupportedOnSqlite", source);
        Assert.Contains("\"DPX038\"", source);
        Assert.Contains("SqliteQueryLockNotSupported", source);
        Assert.Contains("DiagnosticSeverity.Error", source);
    }

    [Fact]
    public void DerivedQueryGenerator_reports_DPX037_for_sqlite_lock_mode_parameter()
    {
        var source = ReadGeneratorSource("Generators/DerivedQueryGenerator.cs");
        Assert.Contains("Diagnostics.LockModeNotSupportedOnSqlite", source);
        Assert.Contains("provider != \"Sqlite\"", source);
    }
}
