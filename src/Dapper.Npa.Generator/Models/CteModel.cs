namespace Dapper.Npa.Generator.Models;
internal sealed class CteModel
{
    public string Name { get; init; } = string.Empty;
    public string BodySql { get; init; } = string.Empty;
    public bool IsRecursive { get; init; }
}
