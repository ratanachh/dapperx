namespace Dapper.Npa.Generator.Models;
internal sealed class SoftDeleteModel
{
    public string Column { get; init; } = "is_deleted";
    public string? DeletedAtColumn { get; init; }
    public string FilterSql => $"{Column} = 0";
    public string BypassFilterSql => "1=1"; // used with IncludeDeleted
}
