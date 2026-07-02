namespace Dapper.Npa.Core.Models;

/// <summary>Informational only — no SQL/DDL generated.</summary>
public sealed class IndexMetadata
{
    public string[] Columns { get; init; } = [];
    public string? Name { get; init; }
    public bool Unique { get; init; }
}
