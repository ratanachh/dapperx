namespace Dapper.Npa.Generator.Models;

internal sealed class AuditingModel
{
    public string? CreatedDateProperty { get; init; }
    public string? LastModifiedDateProperty { get; init; }
    public string? CreatedByProperty { get; init; }
    public string? LastModifiedByProperty { get; init; }
}
