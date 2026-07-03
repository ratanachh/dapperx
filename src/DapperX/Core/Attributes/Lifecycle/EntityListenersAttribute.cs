namespace DapperX.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class EntityListenersAttribute(params Type[] listenerTypes) : Attribute
{
    public Type[] ListenerTypes { get; } = listenerTypes;
}
