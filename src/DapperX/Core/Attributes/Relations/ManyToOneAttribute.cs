using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Declares the owning side of a many-to-one relation to another <c>[Entity]</c>. Pair with <see cref="JoinColumnAttribute"/> to specify the foreign key column.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ManyToOneAttribute : Attribute
{
    /// <summary>Which lifecycle operations on this entity cascade to the related entity.</summary>
    public CascadeType Cascade { get; init; } = CascadeType.None;
    /// <summary>Whether the related entity is loaded eagerly with this one or lazily on first access.</summary>
    public FetchType Fetch { get; init; } = FetchType.Lazy;
}
