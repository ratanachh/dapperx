namespace Dapper.Npa.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class JoinColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public bool Nullable { get; init; } = true;
}
