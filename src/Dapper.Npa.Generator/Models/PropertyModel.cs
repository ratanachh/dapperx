namespace Dapper.Npa.Generator.Models;

internal sealed class PropertyModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string ColumnName { get; init; } = string.Empty;
    public string? SecondaryTable { get; init; }
    public string ClrTypeName { get; init; } = string.Empty;
    public bool IsId { get; init; }
    public bool IsVersion { get; init; }
    public bool IsTransient { get; init; }
    /// <summary>Flattened [Embedded] column — not a CLR property on the entity type.</summary>
    public bool IsEmbeddedColumn { get; init; }
    /// <summary>[Embedded] container property name (e.g. Address).</summary>
    public string? EmbeddedOwner { get; init; }
    /// <summary>Inner embeddable property name (e.g. City).</summary>
    public string? EmbeddedInner { get; init; }
    public bool IsSortable { get; init; }
    public bool Insertable { get; init; } = true;
    public bool Updatable { get; init; } = true;
    public bool Nullable { get; init; } = true;
    public bool IsLazyLoaded { get; init; }
    public string? Formula { get; init; }
    public string? ConverterTypeName { get; init; }
    /// <summary>Database column CLR type from IValueConverter TColumn type argument.</summary>
    public string? ConverterColumnClrTypeName { get; init; }
    public ColumnTransformerModel? ColumnTransformer { get; init; }
    public string? GeneratedTime { get; init; } // "Insert" or "Always"
    public AuditingRole AuditingRole { get; init; }
    public string? IdGenerationStrategy { get; init; } // "Identity","Sequence","Uuid","Assigned"
    public string? SequenceGeneratorName { get; init; }
}

internal enum AuditingRole { None, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy }
