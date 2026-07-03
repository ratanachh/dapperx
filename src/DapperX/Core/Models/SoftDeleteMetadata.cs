namespace DapperX.Core.Models;
public sealed class SoftDeleteMetadata
{
    public string Column { get; init; } = "is_deleted";
    public string? DeletedAtColumn { get; init; }
}
