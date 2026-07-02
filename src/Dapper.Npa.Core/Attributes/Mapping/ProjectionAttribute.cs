namespace Dapper.Npa.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ProjectionAttribute(Type from) : Attribute
{
    public Type From { get; } = from;
}
