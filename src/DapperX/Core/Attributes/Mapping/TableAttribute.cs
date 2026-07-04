namespace DapperX.Core.Attributes;

/// <summary>Maps an <see cref="EntityAttribute"/>-annotated class to a database table.</summary>
/// <param name="name">The table name.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TableAttribute(string name) : Attribute
{
    /// <summary>The table name.</summary>
    public string Name { get; } = name;
    /// <summary>The schema the table lives in, if not the provider's default.</summary>
    public string? Schema { get; init; }
}
