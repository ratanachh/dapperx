namespace Dapper.Npa.Core.Attributes;

/// <summary>Persistent integer position column — generator manages position on insert/delete.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class OrderColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
