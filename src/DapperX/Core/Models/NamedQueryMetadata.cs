namespace DapperX.Core.Models;

public sealed class NamedQueryMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Query { get; init; } = string.Empty;
    public bool NativeQuery { get; init; }
}
