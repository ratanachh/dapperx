namespace Dapper.Npa.Generator.Models;
internal sealed class UpsertModel
{
    public string SqlServer { get; init; } = string.Empty;
    public string PostgreSql { get; init; } = string.Empty;
    public string MySql { get; init; } = string.Empty;
    public string Sqlite { get; init; } = string.Empty;
}
