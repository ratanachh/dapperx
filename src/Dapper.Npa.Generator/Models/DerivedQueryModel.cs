namespace Dapper.Npa.Generator.Models;
internal sealed class DerivedQueryModel
{
    public string MethodName { get; init; } = string.Empty;
    public string BaseSelectSql { get; init; } = string.Empty;
    public string WhereSql { get; init; } = string.Empty;
    public string OrderSql { get; init; } = string.Empty;
    public bool HasSortParam { get; init; }
    public bool HasPageableParam { get; init; }
    public bool IsSlice { get; init; }
    public bool IsPage { get; init; }
    public bool IsStream { get; init; }
    public int? LimitN { get; init; }
}
