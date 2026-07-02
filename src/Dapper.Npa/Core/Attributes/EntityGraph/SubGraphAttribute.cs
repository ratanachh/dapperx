namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SubGraphAttribute(string relationshipProperty) : Attribute
{
    public string RelationshipProperty { get; } = relationshipProperty;
    /// <summary>Named entity graph this subgraph belongs to; when omitted, applies to every graph on the entity.</summary>
    public string? GraphName { get; init; }
    public string[] AttributeNodes { get; init; } = [];
}
