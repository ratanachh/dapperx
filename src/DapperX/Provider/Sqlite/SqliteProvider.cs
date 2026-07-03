using DapperX.Provider.Common;
using DapperX.Core.Enums;

namespace DapperX.Provider.Sqlite;

using DapperX.Core.Enums;
using Provider.Common;

public sealed class SqliteProvider : DatabaseProviderBase
{
    public SqliteProvider()
        : base(DatabaseProvider.Sqlite, new SqliteDialect(), bulkInsertExecutor: null)
    {
    }
}
