namespace Dapper.Npa.Generator.Models;

using System.Linq;

internal sealed class CompositeKeyModel
{
    public string KeyTypeName { get; init; } = string.Empty;
    public bool IsEmbeddedId { get; init; }
    public string? EmbeddedIdPropertyName { get; init; }
    public IReadOnlyList<CompositeKeyPartModel> Parts { get; init; } = [];

    public IReadOnlyList<string> KeyProperties
        => Parts.Select(p => p.KeyClassPropertyName).ToList();

    public IReadOnlyList<(string Property, string Column)> KeyColumns
        => Parts.Select(p => (p.KeyClassPropertyName, p.ColumnName)).ToList();
}
