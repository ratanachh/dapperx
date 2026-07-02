using BenchmarkDotNet.Attributes;
using Dapper.Npa.Performance.Tests.Fixtures;

namespace Dapper.Npa.Performance.Tests;

/// <summary>BenchmarkDotNet types for optional manual runs.</summary>
[MemoryDiagnoser]
public class ResolveColumnBenchmark
{
    [Benchmark]
    public string Resolve_column_switch() => PerfBulkRowRepositoryImpl.ResolveColumn(nameof(PerfBulkRow.Code));
}
