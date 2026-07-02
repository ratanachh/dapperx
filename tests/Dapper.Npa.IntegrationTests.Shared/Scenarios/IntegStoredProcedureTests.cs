using Dapper.Npa.IntegrationTests.Shared.Fixtures;

namespace Dapper.Npa.IntegrationTests.Shared.Scenarios;

#if !DAPPERX_PROVIDER_SQLITE
public class IntegStoredProcedureTests
{
    [Fact]
    public async Task ListOrdersSp_returns_rows()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.ProcOrders.InsertAsync(new IntegProcOrder { Id = 1, Code = "PO-1" });
        var rows = (await env.ProcOrders.ListOrdersSpAsync(99)).ToList();
        Assert.Contains(rows, o => o.Code == "PO-1");
    }

    [Fact]
    public async Task ProcessOrderSp_returns_out_parameters()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.ProcOrders.InsertAsync(new IntegProcOrder { Id = 2, Code = "PO-2" });
        var result = await env.ProcOrders.ProcessOrderSpAsync(2, 10m);
        Assert.Equal(0, result.Value1);
        Assert.Equal("ok", result.Value2);
        Assert.Equal(11m, result.OutputParameters["total"]);
    }

#if DAPPERX_PROVIDER_SQLSERVER || DAPPERX_PROVIDER_MYSQL
    [Fact]
    public async Task GetOrderReportSp_returns_multi_result()
    {
        await using var env = await IntegrationEnvironment.CreateAsync();
        await env.ProcOrders.InsertAsync(new IntegProcOrder { Id = 3, Code = "PO-3" });
        var lineRepo = new global::Dapper.Npa.IntegrationTests.Shared.Fixtures.Generated.IntegProcLineRepositoryImpl(env.Connection, env.Options);
        await lineRepo.InsertAsync(new IntegProcLine { Id = 30, OrderId = 3, Sku = "LINE-30" });

        var report = await env.ProcOrders.GetOrderReportSpAsync(3);
        Assert.Single(report.First);
        Assert.Equal("PO-3", report.First[0].Code);
        Assert.Single(report.Second);
        Assert.Equal("LINE-30", report.Second[0].Sku);
    }
#endif
}
#endif
