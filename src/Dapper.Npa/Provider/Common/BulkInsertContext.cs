namespace Dapper.Npa.Provider.Common;

using System.Data;

public sealed class BulkInsertContext
{
    public required IDbConnection Connection { get; init; }
    public IDbTransaction? Transaction { get; init; }
    public required string TableName { get; init; }
    public required IReadOnlyList<string> ColumnNames { get; init; }
    public required IReadOnlyList<Type> ColumnTypes { get; init; }
    public required IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; }
}
