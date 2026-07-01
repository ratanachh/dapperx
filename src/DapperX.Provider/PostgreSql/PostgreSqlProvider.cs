namespace DapperX.Provider.PostgreSql;

using DapperX.Core.Enums;
using DapperX.Provider.Common;

public sealed class PostgreSqlProvider : DatabaseProviderBase
{
    public PostgreSqlProvider()
        : base(DatabaseProvider.PostgreSql, new PostgreSqlDialect(), PostgreSqlBulkExecutor.Instance)
    {
    }
}
