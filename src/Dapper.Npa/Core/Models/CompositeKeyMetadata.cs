namespace Dapper.Npa.Core.Models;
public sealed class CompositeKeyMetadata
{
    public string KeyTypeName { get; init; } = string.Empty;
    public bool IsEmbeddedId { get; init; }
    public IReadOnlyList<string> KeyProperties { get; init; } = [];
}
