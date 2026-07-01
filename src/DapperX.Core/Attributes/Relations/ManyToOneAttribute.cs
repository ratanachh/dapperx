namespace DapperX.Core.Attributes;

using DapperX.Core.Enums;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ManyToOneAttribute : Attribute
{
    public CascadeType Cascade { get; init; } = CascadeType.None;
    public FetchType Fetch { get; init; } = FetchType.Lazy;
}
