namespace DapperX.Core.Attributes;

using DapperX.Core.Enums;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratedValueAttribute(GenerationType strategy) : Attribute
{
    public GenerationType Strategy { get; } = strategy;
    public string? Generator { get; init; }
}
