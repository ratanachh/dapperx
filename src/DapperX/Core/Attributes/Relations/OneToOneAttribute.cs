using DapperX.Core.Enums;

namespace DapperX.Core.Attributes;

using Core.Enums;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class OneToOneAttribute : Attribute
{
    public CascadeType Cascade { get; init; } = CascadeType.None;
    public FetchType Fetch { get; init; } = FetchType.Lazy;
    public string? MappedBy { get; init; }
}
