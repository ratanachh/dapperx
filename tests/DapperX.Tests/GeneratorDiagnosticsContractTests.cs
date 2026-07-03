namespace DapperX.Tests;

public class GeneratorDiagnosticsContractTests
{
    private static string ReadGeneratorSource(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DapperX.Generator", relativePath));
        return File.ReadAllText(path);
    }

    [Fact]
    public void MetadataBuilder_reports_formula_and_generated_conflict()
    {
        var source = ReadGeneratorSource("Builders/MetadataBuilder.cs");
        Assert.Contains("FormulaOnGeneratedColumn", source);
        Assert.Contains("Diagnostics.FormulaOnGeneratedColumn", source);
    }

    [Fact]
    public void DiagnosticsReporter_defines_column_transformer_and_converter_conflict()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("ColumnTransformerAndConverter", source);
        Assert.Contains("DPX010", source);
    }

    [Fact]
    public void DerivedQueryValidator_reports_include_deleted_without_soft_delete()
    {
        var source = ReadGeneratorSource("Validation/DerivedQueryValidator.cs");
        Assert.Contains("IncludeDeletedWithoutSoftDelete", source);
        Assert.Contains("Diagnostics.IncludeDeletedWithoutSoftDelete", source);
    }
}
