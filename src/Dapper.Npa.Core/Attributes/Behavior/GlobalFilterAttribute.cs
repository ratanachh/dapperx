namespace Dapper.Npa.Core.Attributes;
/// <summary>Compile-time SQL fragment constant; conditional append — never concatenated at runtime.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GlobalFilterAttribute(string name, string condition) : Attribute
{
    public string Name { get; } = name;
    public string Condition { get; } = condition;
}
