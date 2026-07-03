using System.Reflection;
using DapperX.Performance.Tests.Fixtures;

namespace DapperX.Performance.Tests;

/// <summary>Compile-time checks for Requirements.md performance guarantees.</summary>
public class PerformanceRequirementsTests
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
    public void BatchUpdateMany_uses_execute_on_chunk()
    {
        var source = ReadGenerated("PerfBulkRowRepositoryImpl.g.cs");
        var start = source.IndexOf("UpdateManyAsync", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = source[start..];
        Assert.Contains("ExecuteAsync(_connection, UpdateSql, chunk", body);
    }

    [Fact]
    public void BatchInsertMany_assigned_keys_uses_chunk_execute()
    {
        var source = ReadGenerated("PerfBulkRowRepositoryImpl.g.cs");
        var start = source.IndexOf("InsertManyAsync", StringComparison.Ordinal);
        var end = source.IndexOf("UpdateManyAsync", start, StringComparison.Ordinal);
        var body = source[start..end];
        Assert.Contains("ExecuteAsync(_connection, InsertSql, chunk", body);
        Assert.DoesNotContain("ExecuteScalarAsync", body);
    }

    [Fact]
    public void GlobalFilter_paths_avoid_runtime_string_building()
    {
        var source = ReadGenerated("PerfBulkRowRepositoryImpl.g.cs");
        Assert.DoesNotContain("string.Concat", source);
        Assert.DoesNotContain("new StringBuilder", source);
    }

    [Fact]
    public void Repository_has_no_tracking_fields()
    {
        var fields = typeof(PerfBulkRowRepositoryImpl).GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        var unexpected = fields
            .Where(f => f.Name is not ("_connection" or "_options" or "_lifecycle"))
            .ToList();
        Assert.Empty(unexpected);
    }

    [Fact]
    public void LoadForMany_emits_single_in_query()
    {
        var parentSource = ReadGenerated("PerfParentRepositoryImpl.g.cs");
        Assert.Contains("ForManyAsync", parentSource);
        Assert.Contains("IN @parentIds", parentSource);
    }

    [Fact]
    public void Runtime_has_no_cpql_parser_types()
    {
        // DapperX.Core.Enums.CpqlType is a small runtime-facing enum, not parser logic, and
        // legitimately ships in the runtime assembly now that Core/Runtime/etc. are merged into
        // one DapperX assembly. The heavy Roslyn-based CPQL parser/validator must still stay
        // compile-time-only, in DapperX.Generator.
        var runtimeAssembly = typeof(DapperX.Runtime.Query.RepositoryQuery<>).Assembly;
        var cpqlParserTypes = runtimeAssembly.GetTypes()
            .Where(t => t.FullName?.Contains("CpqlTypeHelper", StringComparison.OrdinalIgnoreCase) == true
                || t.FullName?.Contains("CpqlSemanticValidator", StringComparison.OrdinalIgnoreCase) == true)
            .ToList();
        Assert.Empty(cpqlParserTypes);
    }

    [Fact]
    public void Slice_sql_uses_slice_size_without_count()
    {
        var source = ReadGenerated("PerfBulkRowRepositoryImpl.g.cs");
        Assert.Contains("SelectAllSliceSql => \"SELECT", source);
        Assert.Contains("FETCH NEXT @sliceSize ROWS ONLY", source);
    }

    [Fact]
    public void Page_sql_includes_count_query()
    {
        var source = ReadGenerated("PerfBulkRowRepositoryImpl.g.cs");
        Assert.Contains("GetAllAsync(Pageable", source);
        Assert.Contains("CountSql", source);
    }
}
