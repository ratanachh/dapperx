using Microsoft.CodeAnalysis;

namespace DapperX.Generator.Cpql;

internal sealed class CpqlParseResult
{
    public CpqlStatementNode? Ast { get; init; }
    public bool Success => Ast != null && Diagnostics.Count == 0;
    public List<Diagnostic> Diagnostics { get; } = new();

    public static CpqlParseResult Ok(CpqlStatementNode ast) => new() { Ast = ast };

    public static CpqlParseResult Fail(Location? location, string message)
    {
        var result = new CpqlParseResult();
        result.Diagnostics.Add(Diagnostic.Create(
            new DiagnosticDescriptor("DPXCPQL001", "CPQL parse error", message, "DapperX.CPQL", DiagnosticSeverity.Error, true),
            location));
        return result;
    }
}
