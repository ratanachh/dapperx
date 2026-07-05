using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

/// <summary>Declares a one-to-one relation to another <c>[Entity]</c>. Pair with <see cref="JoinColumnAttribute"/> on the owning side, or set <see cref="MappedBy"/> on the inverse side.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class OneToOneAttribute : Attribute
{
    /// <summary>Which lifecycle operations on the owning entity cascade to the related entity.</summary>
    public CascadeType Cascade { get; init; } = CascadeType.None;
    /// <summary>Whether the related entity is loaded eagerly with the owner or lazily on first access.</summary>
    public FetchType Fetch { get; init; } = FetchType.Lazy;
    /// <summary>The property name on the related entity that owns the relation, for the inverse (non-owning) side.</summary>
    public string? MappedBy { get; init; }
}
