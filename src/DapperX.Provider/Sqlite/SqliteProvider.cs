namespace DapperX.Provider.Sqlite;

using DapperX.Core.Enums;
using DapperX.Provider.Common;

public sealed class SqliteProvider : DatabaseProviderBase
{
    public SqliteProvider()
        : base(DatabaseProvider.Sqlite, new SqliteDialect(), bulkInsertExecutor: null)
    {
    }
}
