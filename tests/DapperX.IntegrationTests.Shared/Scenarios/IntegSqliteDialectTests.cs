using Dapper;
using DapperX.IntegrationTests.Shared.Fixtures;

namespace DapperX.IntegrationTests.Shared.Scenarios;

#if DAPPERX_PROVIDER_SQLITE
public class IntegSqliteDialectTests
{
    [Fact]
    public async Task Sqlite_strftime_and_concat_execute()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var value = await env.Connection.ExecuteScalarAsync<string>(
            "SELECT strftime('%Y', '2020-01-01') || '-' || 'ok'");
        Assert.Equal("2020-ok", value);
    }

    [Fact]
    public async Task Insert_assigns_identity_via_last_insert_rowid()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        var item = new IntegCatalogItem { Id = 42, Sku = "id-check" };
        await env.Catalog.InsertAsync(item);
        var count = await env.Connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM integ_catalog WHERE id = 42");
        Assert.Equal(1, count);
    }
}
#endif
