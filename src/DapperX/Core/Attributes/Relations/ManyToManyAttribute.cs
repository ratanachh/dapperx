using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Declares a many-to-many relation to a collection of another <c>[Entity]</c>, backed by a join table. Pair with <see cref="JoinTableAttribute"/> to describe the join table.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ManyToManyAttribute : Attribute
{
    /// <summary>Which lifecycle operations on the owning entity cascade to the related entities.</summary>
    public CascadeType Cascade { get; init; } = CascadeType.None;
    /// <summary>Whether the collection is loaded eagerly with the owner or lazily on first access.</summary>
    public FetchType Fetch { get; init; } = FetchType.Lazy;
}
