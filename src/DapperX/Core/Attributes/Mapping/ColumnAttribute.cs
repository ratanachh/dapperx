using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Maps a property to a table column, controlling its name and DDL/read-write characteristics.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>The column name; defaults to the property name if not set.</summary>
    public string? Name { get; init; }
    /// <summary>Whether the column allows NULL.</summary>
    public bool Nullable { get; init; } = true;
    /// <summary>Whether the column is included in generated INSERT statements.</summary>
    public bool Insertable { get; init; } = true;
    /// <summary>Whether the column is included in generated UPDATE statements.</summary>
    public bool Updatable { get; init; } = true;
    /// <summary>Whether the column has a unique constraint.</summary>
    public bool Unique { get; init; }
    /// <summary>Column length, for string/binary types.</summary>
    public int Length { get; init; }
    /// <summary>Numeric precision, for decimal types.</summary>
    public int Precision { get; init; }
    /// <summary>Numeric scale, for decimal types.</summary>
    public int Scale { get; init; }
    /// <summary>Raw DDL fragment overriding the generated column definition.</summary>
    public string? ColumnDefinition { get; init; }
    /// <summary>Whether the column is loaded eagerly with the rest of the entity or lazily on first access.</summary>
    public FetchType Fetch { get; init; } = FetchType.Eager;
    /// <summary>Secondary table name for <see cref="SecondaryTableAttribute"/> routing.</summary>
    public string? Table { get; init; }
}
