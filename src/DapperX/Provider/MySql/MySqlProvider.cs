using DapperX.Provider.Common;
using DapperX.Core.Enums;

namespace DapperX.Provider.MySql;

using DapperX.Core.Enums;
using Provider.Common;

public sealed class MySqlProvider : DatabaseProviderBase
{
    public MySqlProvider()
        : base(DatabaseProvider.MySql, new MySqlDialect(), MySqlBatchExecutor.Instance)
    {
    }
}
