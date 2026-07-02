namespace Dapper.Npa.Core.Attributes;

/// <summary>SQL-level read/write transformation. '?' in Write is replaced with '@paramName' at compile time.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnTransformerAttribute : Attribute
{
    public string? Read { get; init; }
    public string? Write { get; init; }
}
