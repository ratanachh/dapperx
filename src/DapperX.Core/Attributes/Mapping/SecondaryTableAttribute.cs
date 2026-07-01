namespace DapperX.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SecondaryTableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string PrimaryKeyJoinColumn { get; init; } = string.Empty;
}
