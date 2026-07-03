namespace DapperX.Core.Attributes;

/// <summary>Default ORDER BY clause appended to collection load SQL.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class OrderByAttribute(string clause) : Attribute
{
    public string Clause { get; } = clause;
}
