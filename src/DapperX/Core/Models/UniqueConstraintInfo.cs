namespace DapperX.Core.Models;

/// <summary>Informational only — no SQL/DDL generated.</summary>
public sealed class UniqueConstraintInfo
{
    public string[] Columns { get; init; } = [];
    public string? Name { get; init; }
}
