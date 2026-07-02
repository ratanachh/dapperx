namespace Dapper.Npa.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class AssociationOverrideAttribute(string name, string joinColumn) : Attribute
{
    public string Name { get; } = name;
    public string JoinColumn { get; } = joinColumn;
}
