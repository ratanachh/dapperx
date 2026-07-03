using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    public string? Name { get; init; }
    public bool Nullable { get; init; } = true;
    public bool Insertable { get; init; } = true;
    public bool Updatable { get; init; } = true;
    public bool Unique { get; init; }
    public int Length { get; init; }
    public int Precision { get; init; }
    public int Scale { get; init; }
    public string? ColumnDefinition { get; init; }
    public FetchType Fetch { get; init; } = FetchType.Eager;
    /// <summary>Secondary table name for <see cref="SecondaryTableAttribute"/> routing.</summary>
    public string? Table { get; init; }
}
