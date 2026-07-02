namespace Dapper.Npa.Tests;

/// <summary>Single-project (SqlServer): DPX024 regex-not-supported contract without a non-compiling fixture entity.</summary>
public class SqlServerRegexDiagnosticTests
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
    public void DiagnosticsReporter_defines_DPX024_regex_not_supported()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("DPX024", source);
        Assert.Contains("RegexNotSupportedOnProvider", source);
    }

    [Fact]
    public void Cpql_or_derived_path_rejects_regex_on_sql_server()
    {
        var cpql = ReadGeneratorSource("Generators/CpqlGenerator.cs");
        var derived = ReadGeneratorSource("Generators/DerivedQueryGenerator.cs");
        Assert.True(
            cpql.Contains("DPX024", StringComparison.Ordinal)
            || cpql.Contains("RegexNotSupported", StringComparison.Ordinal)
            || derived.Contains("RegexNotSupported", StringComparison.Ordinal)
            || derived.Contains("BuildRegexPredicate", StringComparison.Ordinal),
            "Expected SqlServer regex guard in generator.");
    }
}
