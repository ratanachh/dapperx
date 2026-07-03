using DapperX.Provider.Common;
using DapperX.Core.Enums;

namespace DapperX.Provider.PostgreSql;

using DapperX.Core.Enums;
using Provider.Common;

public sealed class PostgreSqlProvider : DatabaseProviderBase
{
    public PostgreSqlProvider()
        : base(DatabaseProvider.PostgreSql, new PostgreSqlDialect(), PostgreSqlBulkExecutor.Instance)
    {
    }
}
