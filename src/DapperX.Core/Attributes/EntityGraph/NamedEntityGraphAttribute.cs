namespace DapperX.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NamedEntityGraphAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string[] AttributeNodes { get; init; } = [];
    /// <summary>Collection relationship property names to JOIN as subgraph roots.</summary>
    public string[] SubGraphs { get; init; } = [];
}
