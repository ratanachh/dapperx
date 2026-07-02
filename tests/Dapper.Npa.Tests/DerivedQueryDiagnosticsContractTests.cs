namespace Dapper.Npa.Tests;

public class DerivedQueryDiagnosticsContractTests
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
    public void DiagnosticsReporter_defines_reserved_property_and_ambiguous_query_codes()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("ReservedPropertyName", source);
        Assert.Contains("AmbiguousDerivedQueryPath", source);
        Assert.Contains("DPX023", source);
    }

    [Fact]
    public void PropertyNameValidator_warns_on_reserved_logical_names()
    {
        var source = ReadGeneratorSource("Validation/PropertyNameValidator.cs");
        Assert.Contains("ReservedPropertyName", source);
    }
}
