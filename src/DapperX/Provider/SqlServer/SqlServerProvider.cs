using DapperX.Provider.Common;
using DapperX.Core.Enums;

namespace DapperX.Provider.SqlServer;

using DapperX.Core.Enums;
using Provider.Common;

public sealed class SqlServerProvider : DatabaseProviderBase
{
    public SqlServerProvider()
        : base(DatabaseProvider.SqlServer, new SqlServerDialect(), SqlServerBulkExecutor.Instance)
    {
    }
}
