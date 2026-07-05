namespace DapperX.Core.Attributes;
/// <summary>
/// Enables soft delete for an entity: <c>DeleteAsync</c>/<c>DeleteByIdAsync</c> issue an UPDATE instead of a
/// DELETE, and reads automatically exclude soft-deleted rows unless <c>includeDeleted: true</c> or
/// <c>IncludeDeleted()</c> is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SoftDeleteAttribute : Attribute
{
    /// <summary>The boolean flag column marking a row as deleted.</summary>
    public string Column { get; init; } = "is_deleted";
    /// <summary>Optional timestamp column set to the deletion time.</summary>
    public string? DeletedAtColumn { get; init; }
}
