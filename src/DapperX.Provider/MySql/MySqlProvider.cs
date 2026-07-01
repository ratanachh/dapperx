namespace DapperX.Provider.MySql;

using DapperX.Core.Enums;
using DapperX.Provider.Common;

public sealed class MySqlProvider : DatabaseProviderBase
{
    public MySqlProvider()
        : base(DatabaseProvider.MySql, new MySqlDialect(), MySqlBatchExecutor.Instance)
    {
    }
}
