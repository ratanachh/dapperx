namespace Dapper.Npa.Generator.Models;
internal sealed class FormulaModel
{
    public string PropertyName { get; init; } = string.Empty;
    public string Sql { get; init; } = string.Empty;
    public string ColumnAlias { get; init; } = string.Empty;
}
