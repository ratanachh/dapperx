using Dapper.Npa.Provider.MySql;
using Dapper.Npa.Provider.PostgreSql;
using Dapper.Npa.Provider.Sqlite;
using Dapper.Npa.Provider.SqlServer;

namespace Dapper.Npa.Provider.Common;

using Dapper.Npa.Core.Enums;
using Provider.MySql;
using Provider.PostgreSql;
using Provider.Sqlite;
using Provider.SqlServer;

public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(DatabaseProvider provider)
        => provider switch
        {
            DatabaseProvider.SqlServer => new SqlServerProvider(),
            DatabaseProvider.PostgreSql => new PostgreSqlProvider(),
            DatabaseProvider.MySql => new MySqlProvider(),
            DatabaseProvider.Sqlite => new SqliteProvider(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider."),
        };

    public static IDatabaseProvider Create(string providerName)
        => Create(ParseProviderName(providerName));

    public static IBulkInsertExecutor? GetBulkInsertExecutor(string providerName)
        => Create(providerName).BulkInsertExecutor;

    public static bool SupportsBulkInsert(string providerName)
        => GetBulkInsertExecutor(providerName) is not null;

    public static DatabaseProvider ParseProviderName(string providerName)
        => providerName switch
        {
            "SqlServer" => DatabaseProvider.SqlServer,
            "PostgreSql" => DatabaseProvider.PostgreSql,
            "MySql" => DatabaseProvider.MySql,
            "Sqlite" => DatabaseProvider.Sqlite,
            _ => DatabaseProvider.SqlServer,
        };
}
