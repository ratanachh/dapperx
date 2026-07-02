namespace Dapper.Npa.Tests;

public class ImmutableGeneratorContractTests
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
    public void DiagnosticsReporter_defines_DPX006_mutating_method_on_immutable()
    {
        var source = ReadGeneratorSource("Utils/DiagnosticsReporter.cs");
        Assert.Contains("DPX006", source);
        Assert.Contains("MutatingMethodOnImmutable", source);
    }

    [Fact]
    public void RepositoryEmitter_emits_immutable_mutating_overrides_with_NotSupportedException()
    {
        var source = ReadGeneratorSource("Emitters/RepositoryEmitter.cs");
        Assert.Contains("EmitImmutableMutatingOverrides", source);
        Assert.Contains("Entity is marked [Immutable]", source);
        Assert.Contains("NotSupportedException", source);
    }
}
