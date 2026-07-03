using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace DapperX.SampleApp.Infrastructure;

/// <summary>Resolves sample database provider and connection string (Docker Compose SQL Server or in-memory SQLite).</summary>
public sealed class SampleDatabaseHost
{
    private SampleDatabaseHost(string provider, string connectionString)
    {
        Provider = provider;
        ConnectionString = connectionString;
    }

    public string Provider { get; }

    public string ConnectionString { get; }

    public static SampleDatabaseHost FromConfiguration(IConfiguration configuration)
    {
        var provider = configuration["DapperX:DatabaseProvider"] ?? "SqlServer";
        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var sqlite = configuration.GetConnectionString("Sqlite") ?? "Data Source=:memory:";
            return new SampleDatabaseHost("Sqlite", sqlite);
        }

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is required for SqlServer. Start the database with: docker compose up -d");

        return new SampleDatabaseHost("SqlServer", connectionString);
    }

    public IDbConnection CreateConnection() =>
        Provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)
            ? new SqliteConnection(ConnectionString)
            : new SqlConnection(ConnectionString);
}
