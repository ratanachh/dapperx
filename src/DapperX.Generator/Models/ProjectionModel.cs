namespace DapperX.Generator.Models;

internal sealed class ProjectionModel
{
    public string DtoMetadataName { get; init; } = string.Empty;
    public string DtoDisplayName { get; init; } = string.Empty;
    public string BaseSelectSql { get; init; } = string.Empty;
}
