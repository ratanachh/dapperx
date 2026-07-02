using Dapper.Npa.Core.Enums;

namespace Dapper.Npa.Core.Attributes;

using Core.Enums;

/// <summary>Shorthand for EnumToStringConverter or EnumToIntConverter.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class EnumeratedAttribute(EnumType enumType) : Attribute
{
    public EnumType EnumType { get; } = enumType;
}
