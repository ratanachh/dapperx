namespace Dapper.Npa.Tests;

public class CompositeKeyDiagnosticTests
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
    public void DiagnosticsReporter_defines_DPX030_composite_bulk_id_methods()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("DPX030", source);
        Assert.Contains("CompositeKeyBulkIdMethod", source);
    }

    [Fact]
    public void CompositeKeyGenerator_validates_FindAllById_and_DeleteAllById_on_declared_interface_methods()
    {
        var source = ReadGeneratorSource("Generators/CompositeKeyGenerator.cs");
        Assert.Contains("FindAllByIdAsync", source);
        Assert.Contains("DeleteAllByIdAsync", source);
        Assert.Contains("CompositeKeyBulkIdMethod", source);
    }

    [Fact]
    public void CompositeOrderItemRepositoryImpl_does_not_emit_FindAllByIdAsync()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..",
            "obj", "Generated",
            "Dapper.Npa.Generator", "Dapper.Npa.Generator.DapperXSourceGenerator",
            "CompositeOrderItemRepositoryImpl.g.cs"));
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("FindAllByIdAsync", source);
    }
}
