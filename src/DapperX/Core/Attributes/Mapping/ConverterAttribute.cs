namespace DapperX.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ConverterAttribute(Type converterType) : Attribute
{
    public Type ConverterType { get; } = converterType;
}
