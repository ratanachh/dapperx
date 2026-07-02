using Dapper.Npa.Core.Enums;
using Dapper.Npa.IntegrationTests.Shared.Fixtures;

namespace Dapper.Npa.IntegrationTests.Shared.Scenarios;

#if !DAPPERX_PROVIDER_SQLITE
public class IntegLockTimeoutTests
{
    [Fact]
    public async Task IQuery_WithLock_emits_provider_lock_timeout_sql()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.Catalog.InsertAsync(new IntegCatalogItem { Id = 50, Sku = "LOCK-PROBE" });

        env.SqlCounter.Reset();
        var timeoutMs = ProviderLockTimeoutMs(env.Provider);
        _ = await env.Catalog.Query()
            .Where(x => x.Sku == "LOCK-PROBE")
            .WithLock(LockMode.Pessimistic, timeoutMs)
            .ToListAsync();
        var expectedSqlCalls = env.Provider is "SqlServer" or "PostgreSql" ? 2 : 1;
        env.SqlCounter.AssertSqlCallCount(expectedSqlCalls);

        var sql = string.Join(' ', env.SqlCounter.Entries.Select(e => e.Sql));
        switch (env.Provider)
        {
            case "SqlServer":
                Assert.Contains("LOCK_TIMEOUT", sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("UPDLOCK", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "PostgreSql":
                Assert.Contains("lock_timeout", sql, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("FOR UPDATE", sql, StringComparison.OrdinalIgnoreCase);
                break;
            case "MySql":
                Assert.Contains("NOWAIT", sql, StringComparison.OrdinalIgnoreCase);
                break;
            default:
                throw new InvalidOperationException($"Unknown provider {env.Provider}");
        }
    }

    private static int ProviderLockTimeoutMs(string provider) => provider switch
    {
        "SqlServer" => 5000,
        "PostgreSql" => 1500,
        "MySql" => 3000,
        _ => 0,
    };
}
#endif
