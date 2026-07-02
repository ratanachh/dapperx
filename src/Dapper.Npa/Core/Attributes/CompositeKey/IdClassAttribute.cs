namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IdClassAttribute(Type keyType) : Attribute
{
    public Type KeyType { get; } = keyType;
}
