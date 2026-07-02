namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class CollectionTableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string JoinColumn { get; init; } = string.Empty;
}
