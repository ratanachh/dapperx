namespace Dapper.Npa.Generator.Models;

internal sealed class AssociationOverrideModel
{
    public string RelationshipPropertyName { get; init; } = string.Empty;
    public string OverrideJoinColumn { get; init; } = string.Empty;
}
