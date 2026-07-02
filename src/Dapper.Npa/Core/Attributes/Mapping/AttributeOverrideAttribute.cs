namespace Dapper.Npa.Core.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
public sealed class AttributeOverrideAttribute(string property, string column) : Attribute
{
    public string Property { get; } = property;
    public string Column { get; } = column;
}
