namespace Dapper.Npa.Core.Attributes;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SoftDeleteAttribute : Attribute
{
    public string Column { get; init; } = "is_deleted";
    public string? DeletedAtColumn { get; init; }
}
