namespace Dapper.Npa.Generator.Models;

internal sealed class SecondaryTableModel
{
    public string TableName { get; init; } = string.Empty;
    public string PrimaryKeyJoinColumn { get; init; } = string.Empty;
    public IReadOnlyList<string> PropertyNames { get; init; } = [];
}
