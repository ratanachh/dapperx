using Dapper.Npa.Provider.Common;

namespace Dapper.Npa.Provider.PostgreSql;

using Dapper.Npa.Core.Enums;
using Provider.Common;

public sealed class PostgreSqlProvider : DatabaseProviderBase
{
    public PostgreSqlProvider()
        : base(DatabaseProvider.PostgreSql, new PostgreSqlDialect(), PostgreSqlBulkExecutor.Instance)
    {
    }
}
