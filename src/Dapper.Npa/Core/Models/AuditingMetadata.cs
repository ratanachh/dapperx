namespace Dapper.Npa.Core.Models;

public sealed class AuditingMetadata
{
    public string? CreatedDateProperty { get; init; }
    public string? LastModifiedDateProperty { get; init; }
    public string? CreatedByProperty { get; init; }
    public string? LastModifiedByProperty { get; init; }
}
