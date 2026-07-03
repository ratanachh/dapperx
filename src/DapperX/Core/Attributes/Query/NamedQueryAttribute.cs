namespace DapperX.Core.Attributes;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class NamedQueryAttribute(string name, string query = "") : Attribute
{
    public string Name { get; } = name;
    public string Query { get; } = query;
}
