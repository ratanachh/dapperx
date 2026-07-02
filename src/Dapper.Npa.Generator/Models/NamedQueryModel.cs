namespace Dapper.Npa.Generator.Models;

internal sealed class NamedQueryModel
{
    public string Name { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public bool NativeQuery { get; init; }
}
