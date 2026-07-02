using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Core.Attributes;

using Core.Enums;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratedValueAttribute(GenerationType strategy) : Attribute
{
    public GenerationType Strategy { get; } = strategy;
    public string? Generator { get; init; }
}
