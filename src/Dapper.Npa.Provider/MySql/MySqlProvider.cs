using Dapper.Npa.Provider.Common;

namespace Dapper.Npa.Provider.MySql;

using Dapper.Npa.Core.Enums;
using Provider.Common;

public sealed class MySqlProvider : DatabaseProviderBase
{
    public MySqlProvider()
        : base(DatabaseProvider.MySql, new MySqlDialect(), MySqlBatchExecutor.Instance)
    {
    }
}
