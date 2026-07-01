namespace DapperX.Core.Attributes;

/// <summary>Index documentation hint — informational only, no DDL emitted.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class IndexAttribute(params string[] columns) : Attribute
{
    public string[] Columns { get; } = columns;
    public string? Name { get; init; }
    public bool Unique { get; init; }
}
