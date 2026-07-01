namespace DapperX.Generator.Models;

internal sealed class CompositeKeyPartModel
{
    /// <summary>Property name on the composite key class (e.g. OrderId).</summary>
    public string KeyClassPropertyName { get; init; } = string.Empty;

    /// <summary>Entity property name for IdClass (same as key class); embedded container name for EmbeddedId.</summary>
    public string EntityPropertyName { get; init; } = string.Empty;

    /// <summary>Inner embeddable property when using [EmbeddedId].</summary>
    public string? EmbeddedInnerProperty { get; init; }

    public string ColumnName { get; init; } = string.Empty;
    public string ClrTypeName { get; init; } = string.Empty;
    public string? IdGenerationStrategy { get; init; }
}
