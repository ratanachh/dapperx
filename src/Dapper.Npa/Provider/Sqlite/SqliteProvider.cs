using Dapper.Npa.Provider.Common;

namespace Dapper.Npa.Provider.Sqlite;

using Dapper.Npa.Core.Enums;
using Provider.Common;

public sealed class SqliteProvider : DatabaseProviderBase
{
    public SqliteProvider()
        : base(DatabaseProvider.Sqlite, new SqliteDialect(), bulkInsertExecutor: null)
    {
    }
}
