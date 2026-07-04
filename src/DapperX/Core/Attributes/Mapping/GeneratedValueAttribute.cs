using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Declares how an <see cref="IdAttribute"/>-annotated property's value is produced (e.g. database identity column, sequence).</summary>
/// <param name="strategy">The generation strategy to use.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class GeneratedValueAttribute(GenerationType strategy) : Attribute
{
    /// <summary>The generation strategy to use.</summary>
    public GenerationType Strategy { get; } = strategy;
    /// <summary>The named sequence/generator to use when <see cref="Strategy"/> requires one.</summary>
    public string? Generator { get; init; }
}
