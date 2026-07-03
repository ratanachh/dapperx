namespace DapperX.Generator.Models;
internal sealed class TenancyModel
{
    public string TenantIdColumn { get; init; } = string.Empty;
    public string FilterSql => $"{TenantIdColumn} = @tenantId";
}
