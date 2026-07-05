using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Declares a one-to-many relation to a collection of another <c>[Entity]</c>, loaded as a lazy collection by default.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class OneToManyAttribute : Attribute
{
    /// <summary>Which lifecycle operations on the owning entity cascade to the related entities.</summary>
    public CascadeType Cascade { get; init; } = CascadeType.None;
    /// <summary>Whether the collection is loaded eagerly with the owner or lazily on first access.</summary>
    public FetchType Fetch { get; init; } = FetchType.Lazy;
    /// <summary>The property name on the related entity that owns the relation (the "many" side's foreign key property).</summary>
    public string? MappedBy { get; init; }
}
