namespace DapperX.Core.Models;

using DapperX.Core.Enums;

public sealed class PropertyMetadata
{
    public string PropertyName { get; init; } = string.Empty;
    public string ColumnName { get; init; } = string.Empty;
    public string? SecondaryTable { get; init; }
    public bool IsId { get; init; }
    public bool IsVersion { get; init; }
    public bool IsTransient { get; init; }
    public bool IsSortable { get; init; }
    public bool Insertable { get; init; } = true;
    public bool Updatable { get; init; } = true;
    public bool Nullable { get; init; } = true;
    public bool IsLazyLoaded { get; init; }
    public string? Formula { get; init; }
    public ConverterMetadata? Converter { get; init; }
    public ColumnTransformerMetadata? ColumnTransformer { get; init; }
    public GeneratedMetadata? Generated { get; init; }
    public AuditingRole AuditingRole { get; init; } = AuditingRole.None;
    public GenerationType? IdGeneration { get; init; }
    public string? SequenceGeneratorName { get; init; }
}

public enum AuditingRole { None, CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy }
