namespace DapperX.Generator.MethodNameParsing;

internal sealed class MethodNameParseResult
{
    public ParsedDerivedQuery? Query { get; init; }
    public bool IsAmbiguous { get; init; }
    public bool HasTrailingTokens { get; init; }

    public static MethodNameParseResult Ambiguous() => new() { IsAmbiguous = true };
    public static MethodNameParseResult Trailing() => new() { HasTrailingTokens = true };
    public static MethodNameParseResult Success(ParsedDerivedQuery query) => new() { Query = query };
}
