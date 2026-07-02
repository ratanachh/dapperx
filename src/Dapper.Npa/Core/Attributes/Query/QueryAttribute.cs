namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class QueryAttribute(string query) : Attribute
{
    public string Query { get; } = query;
    public bool NativeQuery { get; init; }
}
