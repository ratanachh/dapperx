using Dapper;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace DapperX.IntegrationTests.SqlServer;

public sealed class SqlServerContainerHealthTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    [Fact]
    public async Task Container_accepts_query()
    {
        await using var connection = new SqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        var value = await connection.ExecuteScalarAsync<int>("SELECT 1");
        Assert.Equal(1, value);
    }
}
