using Dapper.Npa.Provider.Common;

namespace Dapper.Npa.Provider.SqlServer;

using Dapper.Npa.Core.Enums;
using Provider.Common;

public sealed class SqlServerProvider : DatabaseProviderBase
{
    public SqlServerProvider()
        : base(DatabaseProvider.SqlServer, new SqlServerDialect(), SqlServerBulkExecutor.Instance)
    {
    }
}
