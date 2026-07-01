namespace DapperX.Provider.SqlServer;

using DapperX.Core.Enums;
using DapperX.Provider.Common;

public sealed class SqlServerProvider : DatabaseProviderBase
{
    public SqlServerProvider()
        : base(DatabaseProvider.SqlServer, new SqlServerDialect(), SqlServerBulkExecutor.Instance)
    {
    }
}
