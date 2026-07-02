namespace Dapper.Npa.Core.Attributes;

/// <summary>Informational only — no DDL emitted.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class UniqueConstraintAttribute(params string[] columns) : Attribute
{
    public string[] Columns { get; } = columns;
    public string? Name { get; init; }
}
