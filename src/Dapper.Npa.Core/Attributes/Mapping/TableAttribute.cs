namespace Dapper.Npa.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string? Schema { get; init; }
}
