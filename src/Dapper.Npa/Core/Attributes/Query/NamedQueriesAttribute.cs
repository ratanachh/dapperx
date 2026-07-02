namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NamedQueriesAttribute(params NamedQueryAttribute[] queries) : Attribute
{
    public NamedQueryAttribute[] Queries { get; } = queries;
}
