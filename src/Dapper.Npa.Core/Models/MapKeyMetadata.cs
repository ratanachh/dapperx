namespace Dapper.Npa.Core.Models;
public sealed class MapKeyMetadata
{
    public string PropertyName { get; init; } = string.Empty;
    public string KeyColumn { get; init; } = string.Empty;
    public string KeyTypeName { get; init; } = string.Empty;
}
