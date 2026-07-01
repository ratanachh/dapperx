namespace DapperX.Core.Models;

public sealed class SecondaryTableMetadata
{
    public string TableName { get; init; } = string.Empty;
    public string PrimaryKeyJoinColumn { get; init; } = string.Empty;
    public IReadOnlyList<string> PropertyNames { get; init; } = [];
}
