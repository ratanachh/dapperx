namespace DapperX.Core.Models;
public sealed class AssociationOverrideMetadata
{
    public string RelationshipPropertyName { get; init; } = string.Empty;
    public string OverrideJoinColumn { get; init; } = string.Empty;
}
