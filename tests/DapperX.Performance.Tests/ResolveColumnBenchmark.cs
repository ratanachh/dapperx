using BenchmarkDotNet.Attributes;
using DapperX.Performance.Tests.Fixtures;

namespace DapperX.Performance.Tests;

/// <summary>BenchmarkDotNet types for optional manual runs.</summary>
[MemoryDiagnoser]
public class ResolveColumnBenchmark
{
    [Benchmark]
    public string Resolve_column_switch() => PerfBulkRowRepositoryImpl.ResolveColumn(nameof(PerfBulkRow.Code));
}
